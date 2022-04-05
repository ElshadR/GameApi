using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MathematicGameApi.Infrastructure.Containers;
using MathematicGameApi.Infrastructure.Containers.Requests;
using MathematicGameApi.Infrastructure.Containers.Responses;
using MathematicGameApi.Infrastructure.Db;
using MathematicGameApi.Infrastructure.Enums;
using MathematicGameApi.Infrastructure.Extensions;
using MathematicGameApi.Infrastructure.Models;
using MathematicGameApi.Infrastructure.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace MathematicGameApi.Infrastructure.Services.Implementations
{
    public class CoreService : ICoreService
    {
        private MathematicGameDbContext _db { get; set; }
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContext;
        int oneLifeTime = 10;

        public CoreService(MathematicGameDbContext db, IConfiguration configuration, IHttpContextAccessor httpContext)
        {
            _db = db;
            _configuration = configuration;
            _httpContext = httpContext;
        }


        /// <summary>
        /// tokene gore claims dan kullanicinin bilgilerini goturuluyor
        /// </summary>
        /// <returns></returns> 
        private (int userId, string userName) GetUserId()
        {
            var currentUser = _httpContext?.HttpContext?.User;

            if (currentUser == null || !currentUser.HasClaim(c => c.Type == "UserName") ||
                !currentUser.HasClaim(c => c.Type == "UserId"))
                return (0, string.Empty);
            var userIdClaims = currentUser.Claims.FirstOrDefault(x => x.Type == "UserId");
            var userNameClaims = currentUser.Claims.FirstOrDefault(x => x.Type == "UserName");

            return !int.TryParse(userIdClaims?.Value, out var userId)
                ? (0, string.Empty)
                : (userId, userNameClaims?.Value);
        }

        /// <summary>
        /// username-e gore yoxlayiriq ki, bele bir user varmi
        /// </summary>
        ///// <param name="userName"></param>
        ///// <param name="email"></param>
        ///// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> CheckUserName(string userName)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName);

                return user != null;
            }
            catch (Exception e)
            {
                //message add log
                throw;
            }
        }

        /// <summary>
        /// kullanici register oluyor ve kullanici adlari olusturuluyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<RegisterResponse>> Register(string userName, string email, string password)
        {
            var response = new RegisterResponse();
            var answer = new AnswerResponse<RegisterResponse>();
            answer.Data = new RegisterResponse();
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(x =>
                    x.UserName == userName || x.Email.ToLower() == email.ToLower());

                if (user != null)
                {
                    answer.Code = ResultCodes.Exist;
                    return answer;
                }

                password.CreatePasswordHash(out var passwordHash, out var passwordSalt);

                await _db.Users.AddAsync(new User()
                {
                    UserName = userName,
                    Email = email,
                    AddedDate = DateTime.Now.AddHours(-1),
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Life = 10
                });

                await _db.SaveChangesAsync();
                response.UserName = userName;
                answer.Data = response;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                //message add log
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        /// userler username'a gore login oldukdan sonra geriye jwt token donuyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<LoginResponse>> Login(string userName, string password)
        {
            var answer = new AnswerResponse<LoginResponse>();
            answer.Data = new LoginResponse();
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName);

                if (user == null)
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                if (!password.VerifyPasswordHash(user.PasswordHash, user.PasswordSalt))
                {
                    answer.Code = ResultCodes.PasswordInvalid;
                    return answer;
                }

                var claims = new[]
                {
                    new Claim("UserName", user.UserName),
                    new Claim("UserId", user.Id.ToString()),
                };
                var x = _configuration["AuthSettings:Key"];

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(x));

                var jwt = new JwtSecurityToken(
                    issuer: _configuration["AuthSettings:Insuer"],
                    audience: _configuration["AuthSettings:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMonths(1),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

                answer.Data.Token = new JwtSecurityTokenHandler().WriteToken(jwt);
                user.UserPosition = UserPosition.Online;
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///oyuna baslamayan odalarin listesini cikariyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<List<RoomResponse>>> GetRooms(int page, int count)
        {
            var answer = new AnswerResponse<List<RoomResponse>>();
            try
            {
                var removeRooms = await _db.Rooms.Include(x => x.UserRooms).Where(x => x.UserRooms.Count() <= 0).ToListAsync();
                _db.Rooms.RemoveRange(removeRooms);
                await _db.SaveChangesAsync();

                var rooms = await _db.Rooms
                    .Where(x => x.StartinDate == null)
                    .OrderByDescending(x => x.AddedDate)
                    .Skip((page - 1) * count)
                    .Take(count)
                    .Select(x => new RoomResponse()
                    {
                        Id = x.Id,
                        Type = x.Type,
                        UserCount = x.UserCount,
                        CreatedUserId = x.CreatedUserId
                    }).ToListAsync();
                foreach (var x in rooms)
                {
                    var user = await _db.Users.FindAsync(x.CreatedUserId);
                    x.CurrentUserCount = await _db.UserRooms.CountAsync(y => y.RoomId == x.Id);
                    x.CreatedUser = new UserResponse()
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        UserPosition = user.UserPosition,
                    };
                  
                }
            
                answer.Code = ResultCodes.Ok;
                answer.Data = rooms;

            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        /// odada olan kullanicilarin listesini cikariyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<RoomResponse>> GetRoomDetails(int roomId)
        {
            var answer = new AnswerResponse<RoomResponse>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                var room = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == roomId);

                if (room == null)
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }


                var users = await _db.UserRooms
                    .Include(x => x.User)
                    .Where(x => x.RoomId == roomId)
                    .Select(x => new UserResponse()
                    {
                        Id = x.UserId,
                        UserName = x.User.UserName,
                        UserPosition = x.User.UserPosition,
                        Photo = x.User.Photo,
                        Level = x.User.Level,
                        Score = x.User.Score,
                    }).ToListAsync();

                foreach (var item in users)
                {
                    item.LevelName = CalculateLevel(item.Score).levelName;
                }

                var data = new RoomResponse()
                {
                    Id = room.Id,
                    UserCount = room.UserCount,
                    CurrentUserCount = users.Count,
                    Type = room.Type,
                    AtRoomUsers = users,
                };

                
                answer.Code = ResultCodes.Ok;
                answer.Data = data;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///kullanicin arkadaslarini geri donderiyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<List<UserResponse>>> GetUserFriends(int page, int count)
        {
            var answer = new AnswerResponse<List<UserResponse>>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                var friends = await (from uf in _db.UserFriends
                                     join u in _db.Users on uf.FriendId equals u.Id
                                     where uf.UserId == userInfo.userId && uf.UserFriendStatus == UserFriendStatus.Approve
                                     select new UserResponse()
                                     {
                                         Id = u.Id,
                                         UserName = u.UserName,
                                         Email = u.Email,
                                         Level = u.Level,
                                         Score = u.Score,
                                         UserPosition = u.UserPosition,
                                         Photo = u.Photo,
                                     })
                    .Take(count)
                    .Skip((page - 1) * count)
                    .ToListAsync();

                friends
                    .ForEach(x => { x.LevelName = CalculateLevel(x.Score).levelName; });
                answer.Data = friends;
                answer.Code = ResultCodes.Ok;
            }

            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///oda yaratiyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<RoomResponse>> CreateRoom(int userCount, RoomType type)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var answer = new AnswerResponse<RoomResponse>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var room = new Room()
                {
                    UserCount = userCount,
                    CreatedUserId = userId,
                    Type = type,
                    AgainKey = Guid.NewGuid().ToString(),
                };
                _db.Rooms.Add(room);
                await _db.SaveChangesAsync();

                var newRoom = _db.UserRooms
                    .Add(new UserRoom()
                    {
                        UserId = userId,
                        RoomId = room.Id
                    });
                await _db.SaveChangesAsync();

                answer.Data = new RoomResponse()
                {
                    Id = room.Id,
                    Type = room.Type,
                    UserCount = room.UserCount
                };
                answer.Code = ResultCodes.Ok;
                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///yeniden oynamak icin
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<int>> AgainPlay(int userCount, RoomType type, string againKey)
        {
            var answer = new AnswerResponse<int>();
            try
            {
                int result = 0;
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var checkRoom = await _db.Rooms.FirstOrDefaultAsync(x => x.AgainKey == againKey);

                if (checkRoom == null)
                {
                    var room = new Room()
                    {
                        UserCount = userCount,
                        CreatedUserId = userId,
                        Type = type,
                        AgainKey = againKey
                    };
                    _db.Rooms.Add(room);
                    await _db.SaveChangesAsync();
                    result = room.Id;
                    _db.UserRooms.Add(new UserRoom()
                    {
                        UserId = userId,
                        RoomId = room.Id
                    });
                    await _db.SaveChangesAsync();
                }
                else
                {
                    _db.UserRooms.Add(new UserRoom()
                    {
                        UserId = userId,
                        RoomId = checkRoom.Id
                    });
                    await _db.SaveChangesAsync();
                    result = checkRoom.Id;
                }

                answer.Data = result;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///kullanicinin bilgilerini veriyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<UserResponse>> GetUserInfo()
        {
            var answer = new AnswerResponse<UserResponse>();
            var response = new UserResponse();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var user = await _db.Users
                    .Include(x => x.UserFriends)
                    .Include(x => x.Rooms)
                    .FirstOrDefaultAsync(x => x.Id == userId);
                response.Id = user.Id;
                response.UserName = user.UserName;
                response.Email = user.Email;
                response.Level = user.Level;
                response.Life = user.Life;
                response.LevelName = CalculateLevel(user.Score).levelName;
                response.Score = user.Score;
                response.UserPosition = user.UserPosition;
                response.Photo = user.Photo;
                response.UserFriends = user.UserFriends.Where(x => x.UserFriendStatus == UserFriendStatus.Approve)
                    .Select(x => new UserResponse()
                    {
                        Id = x.FriendId,
                        UserName = _db.Users.FirstOrDefault(y => y.Id == x.FriendId)?.UserName,
                        Photo = _db.Users.FirstOrDefault(y => y.Id == x.FriendId)?.Photo
                    }).ToList();
                answer.Data = response;
                answer.Code = ResultCodes.Ok;
            }

            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///kullanicilari aratmak
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<List<UserResponse>>> SearchUsers(string userName, int page)
        {
            var count = 20;
            var answer = new AnswerResponse<List<UserResponse>>();
            var response = new List<UserResponse>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var users = await _db.Users
                    .Where(x => string.IsNullOrEmpty(userName) ||
                                x.UserName.ToLower().Contains(userName.ToLower()) && x.Id != userInfo.userId)
                    .Skip((page - 1) * count)
                    .Take(count)
                    .ToListAsync();

                response = users.Select(user => new UserResponse()
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Level = user.Level,
                    LevelName = CalculateLevel(user.Score).levelName,
                    Score = user.Score,
                    UserPosition = user.UserPosition,
                    Photo = user.Photo
                }).ToList();
                answer.Data = response;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///top kullanicilari geri donderiyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<List<TopUserResponse>>> TopUsers(int count)
        {
            var answer = new AnswerResponse<List<TopUserResponse>>();
            var response = new List<TopUserResponse>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var currentUser = await _db.Users.FindAsync(userInfo.userId);
                var users = await _db.Users
                    .OrderByDescending(x => x.Score)
                    .Take(count)
                    .ToListAsync();

                if (users.Any(x => x.Id == userInfo.userId))
                {
                    response = users
                        .Select(user => new TopUserResponse()
                        {
                            Id = user.Id,
                            UserName = user.UserName,
                            Level = user.Level,
                            LevelName = CalculateLevel(user.Score).levelName,
                            Score = user.Score.ToString(),
                        })
                        .ToList();

                    var place = 1;
                    response.ForEach(x => { x.Place = (place++).ToString(); });
                }
                else
                {
                    response = users
                        .Take(users.Count == count ? users.Count - 1 : users.Count)
                        .Select(user => new TopUserResponse()
                        {
                            Id = user.Id,
                            UserName = user.UserName,
                            Level = user.Level,
                            LevelName = CalculateLevel(user.Score).levelName,
                            Score = user.Score == 0 ? "" : user.Score.ToString(),
                        }).ToList();

                    var currentUserPlace = await _db.Users
                        .OrderByDescending(x => x.Score)
                        .Where(x => x.Score >= currentUser.Score).CountAsync();

                    response.Add(new TopUserResponse
                    {
                        Place = ".............",
                        Score = "",
                        UserName = ""
                    });
                    response.Add(new TopUserResponse
                    {
                        Id = currentUser.Id,
                        UserName = currentUser.UserName,
                        Level = currentUser.Level,
                        LevelName = CalculateLevel(currentUser.Score).levelName,
                        Score = currentUser.Score == 0 ? "" : currentUser.Score.ToString(),
                    });

                    var place = 1;
                    response.ForEach(x =>
                    {
                        if (x.Id == userInfo.userId)
                            x.Place = currentUserPlace.ToString();
                        else if (x.Place != ".............")
                            x.Place = (place++).ToString();
                    });
                }


                answer.Data = response;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///arkadas ekleme sorgusu gondermek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> RequestFriend(int id)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var userFriend = await _db.UserFriends
                    .FirstOrDefaultAsync(x => x.UserId == userInfo.userId && x.FriendId == id);

                if (userFriend == null)
                {
                    _db.UserFriends.Add(new UserFriend()
                    {
                        FriendId = id,
                        UserId = userInfo.userId,
                        UserFriendStatus = UserFriendStatus.Request
                    });
                    ;
                    await _db.SaveChangesAsync();
                    answer.Data = true;
                    answer.Code = ResultCodes.Ok;
                }
                else
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.Exist;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///gonderilen arkadas istegini kabul etmek yada redd etmek 
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> ApproveOrCancelFriendRequest(int id, UserFriendStatus status)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var userFriendRequest = await _db.UserFriends
                    .FirstOrDefaultAsync(x =>
                        x.UserId == id && x.FriendId == userInfo.userId &&
                        x.UserFriendStatus == UserFriendStatus.Request);
                if (userFriendRequest != null)
                {
                    if (status == UserFriendStatus.Approve)
                    {
                        userFriendRequest.UserFriendStatus = UserFriendStatus.Approve;
                        _db.UserFriends.Add(new UserFriend()
                        {
                            FriendId = id,
                            UserId = userInfo.userId,
                            UserFriendStatus = UserFriendStatus.Approve
                        });
                    }
                    else
                    {
                        _db.UserFriends.Remove(userFriendRequest);
                    }

                    await _db.SaveChangesAsync();
                    answer.Data = true;
                    answer.Code = ResultCodes.Ok;
                }
                else
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.Exist;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        public async Task<AnswerResponse<bool>> ApproveOrCancelPlayRequest(int id, UserPlayRequest status)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var userPlayRequest = await _db.InviteFriends.OrderBy(x => x.AddedDate)
                    .FirstOrDefaultAsync(x => x.UserId == id);


                if (userPlayRequest != null)
                {
                    if (status == UserPlayRequest.Approve)
                    {
                        await AddUserToRoom(userPlayRequest.RoomId);
                        answer.Data = true;
                        answer.Code = ResultCodes.PlayInviteAccept;
                    }
                    else
                    {
                        answer.Data = false;
                        answer.Code = ResultCodes.PlayInviteReject;
                    }

                    _db.InviteFriends.Remove(userPlayRequest);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.NotFound;
                }

                if (userPlayRequest != null) _db.InviteFriends.Remove(userPlayRequest);
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }


        public async Task<AnswerResponse<bool>> CheckPosition(int id, UserPosition status)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var friends = await (from uf in _db.UserFriends
                                     join u in _db.Users on uf.FriendId equals u.Id
                                     where uf.UserId == userInfo.userId && u.UserPosition == UserPosition.Online &&
                                           uf.UserFriendStatus == UserFriendStatus.Approve
                                     select new UserResponse()
                                     {
                                         Id = u.Id,
                                         UserName = u.UserName,
                                         Email = u.Email,
                                         Level = u.Level,
                                         Score = u.Score,
                                         UserPosition = u.UserPosition,
                                         Photo = u.Photo,
                                     }).ToListAsync();

                friends.ForEach(x => { x.LevelName = CalculateLevel(x.Score).levelName; });

                if (friends != null)
                {
                    if (status == UserPosition.Online)
                    {
                        answer.Data = true;
                        answer.Code = ResultCodes.Ok;
                    }
                    else if (status == UserPosition.Offline)
                    {
                        answer.Data = true;
                        answer.Code = ResultCodes.Ok;
                    }
                    else if (status == UserPosition.AtGame)
                    {
                        answer.Data = true;
                        answer.Code = ResultCodes.Ok;
                    }

                    await _db.SaveChangesAsync();
                }
                else
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.NotFound;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        public async Task<AnswerResponse<bool>> AddUserPosition(UserPosition position)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId != 0)
                {
                    var user = await _db.Users.FindAsync(userInfo.userId);
                    user.UserPosition = position;

                    answer.Data = true;
                    answer.Code = ResultCodes.Ok;
                    return answer;
                }
                else
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///kullanicin arkadaslarini geri donderiyor
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<List<UserResponse>>> GetUserFriendsRoom(int page, int count)
        {
            var answer = new AnswerResponse<List<UserResponse>>();
            try
            {
                var userInfo = GetUserId();

                if (userInfo.userId == 0)
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                var friends = await (from uf in _db.UserFriends
                                     join u in _db.Users on uf.FriendId equals u.Id
                                     where uf.UserId == userInfo.userId && u.UserPosition == UserPosition.Online &&
                                           uf.UserFriendStatus == UserFriendStatus.Approve
                                     select new UserResponse()
                                     {
                                         Id = u.Id,
                                         UserName = u.UserName,
                                         Email = u.Email,
                                         Level = u.Level,
                                         Score = u.Score,
                                         UserPosition = u.UserPosition,
                                         Photo = u.Photo,
                                     })
                    .Take(count)
                    .Skip((page - 1) * count)
                    .ToListAsync();

                friends.ForEach(x => { x.LevelName = CalculateLevel(x.Score).levelName; });
                answer.Data = friends;
                answer.Code = ResultCodes.Ok;
            }

            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }


        /// <summary>
        ///odaya kullanici eklemek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> AddUserToRoom(int roomId)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                //
                var atRoomUsers = await _db.UserRooms.Where(y => y.RoomId == roomId).ToListAsync();
                if (atRoomUsers.Any(x => x.UserId == userId))
                {
                    answer.Data = true;
                    answer.Code = ResultCodes.Ok;
                    return answer;
                }

                var roomResult = await GetRoomDetails(roomId);
                if (roomResult.Data.CurrentUserCount < roomResult.Data.UserCount)
                {
                    _db.UserRooms.Add(new UserRoom()
                    {
                        UserId = userId,
                        RoomId = roomId
                    });
                    answer.Data = true;
                    answer.Code = ResultCodes.Ok;
                }
                else
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.RoomIsFull;
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///odadan kullanicini silmek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> DeleteUserFromRoom(int roomId)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var entry = _db.UserRooms.FirstOrDefault(x => x.UserId == userId && x.RoomId == roomId);
                if (entry != null)
                {
                    _db.UserRooms.Remove(entry);
                    await _db.SaveChangesAsync();
                }

                answer.Data = true;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///oda id sine gore oyunu baslatmak
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> StartedGameByRoomId(int roomId)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var entry = await _db.Rooms.FindAsync(roomId);
                if (entry is { StartinDate: null })
                {
                    entry.StartinDate = DateTime.Now.AddHours(-1);
                    var roomUsers = await GetRoomDetails(roomId);
                    roomUsers.Data.AtRoomUsers.ForEach(x => { x.UserPosition = UserPosition.AtGame; });
                    await _db.SaveChangesAsync();
                    answer.Data = true;
                }

                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///kullaniciya siradaki soruyu gondermek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<QuestionResponse>> GetNextQuestion(int variantId, int roomId, int iQuestion)
        {
            var answer = new AnswerResponse<QuestionResponse>();
            var response = new QuestionResponse();
            try
            {
                var (userId, _) = GetUserId();
                var room = await _db.Rooms.FindAsync(roomId);
                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                if (iQuestion == 1)
                {
                    var firstQuestion = await _db.Questions
                        .Include(x => x.Variants)
                        .OrderBy(x => x.AddedDate)
                        .FirstOrDefaultAsync(x => x.RoomId == roomId);

                    if (firstQuestion != null)
                    {
                        response.Id = firstQuestion.Id;
                        response.Text = firstQuestion.Text;
                        response.VariantResponses = firstQuestion.Variants.Select(x => new VariantResponse()
                        {
                            Text = x.Text,
                            Id = x.Id,
                            IsAnswer = x.IsAnswer
                        }).ToList();
                        answer.Data = response;
                        answer.Code = ResultCodes.Ok;
                        return answer;
                    }
                    /// <summary>
                    ///soruyu olusturmak
                    /// </summary>
                    /// <returns></returns>


                    var questionGenerate = GenerateQuestion(room.Type);
                    var question = new Question()
                    {
                        Text = questionGenerate.Text,
                        RoomId = roomId
                    };
                    _db.Questions.Add(question);
                    await _db.SaveChangesAsync();
                    var variants = questionGenerate.VariantResponses.Select(x => new Variant()
                    {
                        Text = x.Text,
                        IsAnswer = x.IsAnswer,
                        QuestionId = question.Id
                    }).ToList();
                    _db.Variants.AddRange(variants);
                    await _db.SaveChangesAsync();
                    response.Id = question.Id;
                    response.Text = question.Text;
                    response.VariantResponses = variants.Select(x => new VariantResponse()
                    {
                        Id = x.Id,
                        Text = x.Text,
                        IsAnswer = x.IsAnswer
                    }).ToList();
                    response.CurrentCorrectVariantId = response.VariantResponses.First(x => x.IsAnswer == true).Id;
                    answer.Data = response;
                    answer.Code = ResultCodes.Ok;
                }
                else
                {
                    _db.UserQuestionHistories.Add(new UserQuestionHistory()
                    { UserId = userId, VariantId = variantId, AddedDate = DateTime.Now.AddHours(-1) });
                    await _db.SaveChangesAsync();

                    var calculateScore = await CalculateScore(roomId, userId);
                    if (calculateScore.Code == ResultCodes.Ok)
                    {
                        var first = calculateScore.Data.List.FirstOrDefault(x => x.UserId == userId);

                        if (first != null) response.Score = first.Score;
                    }

                    var roomQuestions = await _db.Questions
                        .Include(x => x.Variants)
                        .OrderBy(x => x.AddedDate)
                        .Where(x => x.RoomId == roomId).ToListAsync();

                    var variant = await _db.Variants.FindAsync(variantId);
                    var correctVariant = (await _db.Questions.Include(x => x.Variants)
                            .FirstOrDefaultAsync(x => x.Id == variant.QuestionId)).Variants
                        .FirstOrDefault(x => x.IsAnswer == true);

                    if (roomQuestions.Count() + 1 <= iQuestion)
                    {
                        var questionGenerate = GenerateQuestion(room.Type);
                        var question = new Question()
                        {
                            Text = questionGenerate.Text,
                            RoomId = roomId
                        };
                        _db.Questions.Add(question);
                        await _db.SaveChangesAsync();
                        var variants = questionGenerate.VariantResponses.Select(x => new Variant()
                        {
                            Text = x.Text,
                            IsAnswer = x.IsAnswer,
                            QuestionId = question.Id
                        }).ToList();
                        _db.Variants.AddRange(variants);
                        await _db.SaveChangesAsync();
                        response.Id = question.Id;
                        response.Text = question.Text;
                        response.VariantResponses = variants.Select(x => new VariantResponse()
                        {
                            Id = x.Id,
                            Text = x.Text,
                            IsAnswer = x.IsAnswer
                        }).ToList();
                    }
                    else
                    {
                        var question = roomQuestions
                            .OrderBy(x => x.AddedDate)
                            .Skip(iQuestion - 1).First();

                        response.Id = question.Id;
                        response.Text = question.Text;
                        response.VariantResponses = question.Variants.Select(x => new VariantResponse()
                        {
                            Id = x.Id,
                            Text = x.Text,
                            IsAnswer = x.IsAnswer
                        }).ToList();
                    }

                    if (correctVariant != null) response.BeforeCorrectVariantId = correctVariant.Id;
                    response.CurrentCorrectVariantId = response.VariantResponses.First(x => x.IsAnswer == true).Id;
                    answer.Data = response;
                    answer.Code = ResultCodes.Ok;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        private async Task<AnswerResponse<EndedGameResponse>> CalculateScore(int roomId, int userId)
        {
            var answer = new AnswerResponse<EndedGameResponse>();
            var response = new EndedGameResponse
            {
                List = new List<EndedGameItemResponse>()
            };
            try
            {
                var entry = await _db.Rooms.FindAsync(roomId);
                if (entry != null)
                {
                    entry.EndedDate = DateTime.Now.AddHours(-1);
                    await _db.SaveChangesAsync();

                    var usersRoom = await _db.UserRooms
                        .Include(x => x.User)
                        .Where(x => x.RoomId == roomId).ToListAsync();

                    foreach (var userRoom in usersRoom)
                    {
                        var questionHistories = await _db.UserQuestionHistories
                            .Include(x => x.Variant)
                            .Include(x => x.Variant.Question)
                            .Where(x => x.UserId == userRoom.UserId && x.Variant.Question.RoomId == roomId)
                            .ToListAsync();
                        response.List.Add(new EndedGameItemResponse()
                        {
                            UserId = userRoom.User.Id,
                            UserName = userRoom.User?.UserName,
                            CorrectAnswer = questionHistories.Count(x => x.Variant.IsAnswer),
                            WrongAnswer = questionHistories.Count(x => !x.Variant.IsAnswer),
                            Photo = userRoom.User?.Photo,
                        });
                    }

                    response.List.ForEach(x =>
                    {
                        var m = x.WrongAnswer / 4;
                        if (entry.Type == RoomType.Kolay)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 5 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 7 : (x.CorrectAnswer - m) * 11;
                        }
                        else if (entry.Type == RoomType.Normal)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 10 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 12 : (x.CorrectAnswer - m) * 16;
                        }
                        else if (entry.Type == RoomType.Zor)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 15 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 17 : (x.CorrectAnswer - m) * 21;
                        }
                        else if (entry.Type == RoomType.Uzman)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 20 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 22 : (x.CorrectAnswer - m) * 26;
                        }

                        if (x.Score < 0)
                            x.Score = 0;
                    });

                    response.List = response.List.OrderByDescending(x => x.Score).ToList();
                    var currentUserScore = response.List.FirstOrDefault(x => x.UserId == userId);
                    foreach (var item in response.List)
                    {
                        if (item.UserId == currentUserScore.UserId)
                            item.LevelName = CalculateLevel(item.Score + currentUserScore.Score).levelName;
                        else
                            item.LevelName = CalculateLevel(item.Score).levelName;
                    }

                    answer.Data = response;
                    answer.Code = ResultCodes.Ok;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///oda id sine gore oyunun sonlandirilmasi
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<EndedGameResponse>> EndedGameByRoomId(int roomId)
        {
            var answer = new AnswerResponse<EndedGameResponse>();
            var response = new EndedGameResponse
            {
                List = new List<EndedGameItemResponse>()
            };
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var entry = await _db.Rooms.FindAsync(roomId);
                if (entry != null)
                {
                    entry.EndedDate = DateTime.Now.AddHours(-1);
                    await _db.SaveChangesAsync();

                    var usersRoom = await _db.UserRooms
                        .Include(x => x.User)
                        .Where(x => x.RoomId == roomId).ToListAsync();

                    foreach (var userRoom in usersRoom)
                    {
                        var questionHistories = await _db.UserQuestionHistories
                            .Include(x => x.Variant)
                            .Include(x => x.Variant.Question)
                            .Where(x => x.UserId == userRoom.UserId && x.Variant.Question.RoomId == roomId)
                            .ToListAsync();
                        response.List.Add(new EndedGameItemResponse()
                        {
                            UserId = userRoom.User.Id,
                            UserName = userRoom.User?.UserName,
                            CorrectAnswer = questionHistories.Count(x => x.Variant.IsAnswer),
                            WrongAnswer = questionHistories.Count(x => !x.Variant.IsAnswer),
                            Photo = userRoom.User?.Photo,
                        });
                    }
                    /// <summary>
                    ///skorlarin hesaplasnmasi
                    /// </summary>
                    /// <returns></returns>


                    response.List.ForEach(x =>
                    {
                        var m = x.WrongAnswer / 4;
                        if (entry.Type == RoomType.Kolay)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 5 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 7 : (x.CorrectAnswer - m) * 11;
                        }
                        else if (entry.Type == RoomType.Normal)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 10 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 12 : (x.CorrectAnswer - m) * 16;
                        }
                        else if (entry.Type == RoomType.Zor)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 15 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 17 : (x.CorrectAnswer - m) * 21;
                        }
                        else if (entry.Type == RoomType.Uzman)
                        {
                            x.Score = entry.UserCount == 2 ? (x.CorrectAnswer - m) * 20 :
                                entry.UserCount == 3 ? (x.CorrectAnswer - m) * 22 : (x.CorrectAnswer - m) * 26;
                        }

                        if (x.Score < 0)
                            x.Score = 0;
                    });

                    response.List = response.List.OrderByDescending(x => x.Score).ToList();
                    response.AgainKey = entry.AgainKey;
                    entry.ResponseCount++;
                    await _db.SaveChangesAsync();
                    var currentUserScore = response.List.FirstOrDefault(x => x.UserId == userId);
                    foreach (var item in response.List)
                    {
                        if (item.UserId == currentUserScore.UserId)
                            item.LevelName = CalculateLevel(item.Score + currentUserScore.Score).levelName;
                        else
                            item.LevelName = CalculateLevel(item.Score).levelName;
                    }

                    if (userId == currentUserScore.UserId && entry.ResponseCount == response.List.Count)
                    {
                        var currentUser = await _db.Users.FindAsync(userId);
                        var maxScore = 0;
                        foreach (var item in response.List)
                        {
                            if (maxScore < item.Score)
                            {
                                maxScore = item.Score;
                            }
                        }

                        foreach (var item in response.List.Where(x => x.Score != maxScore))
                        {
                            var user = await _db.Users.FindAsync(item.UserId);
                            user.Life--;
                            double restOfTime = (user.LifeTime + oneLifeTime);
                            user.LastGameTime = DateTime.Now.AddHours(-1);
                            user.LifeTime = restOfTime;
                        }

                        foreach (var item in response.List.Where(x => x.Score == maxScore))
                        {
                            var user = await _db.Users.FindAsync(item.UserId);
                            user.Score += currentUserScore.Score;
                            user.Level = CalculateLevel(currentUser.Score).level;
                        }

                        var roomUsers = await GetRoomDetails(roomId);
                        roomUsers.Data.AtRoomUsers.ForEach(x => { x.UserPosition = UserPosition.Online; });

                        _db.Rooms.Remove(entry);
                        await _db.SaveChangesAsync();

                        var questions = await _db.Questions.Where(x => x.RoomId == roomId).ToListAsync();
                        _db.Questions.RemoveRange(questions);
                        await _db.SaveChangesAsync();
                    }

                    answer.Data = response;
                    answer.Code = ResultCodes.Ok;
                }
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        /// <summary>
        ///kullanicini ismini ve resmini degistirmek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> UpdateUser(int id, string userName, IFormFile photo)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var userFind = await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName);

                if (userFind != null)
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.Exist;
                }

                var filePath = string.Empty;
                if (photo != null)
                    filePath = await photo.Save(Directory.GetCurrentDirectory(), "Files", "Images");

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);

                if (!string.IsNullOrEmpty(userName))
                    user.UserName = userName;

                if (!string.IsNullOrEmpty(filePath))
                    user.Photo = filePath;

                await _db.SaveChangesAsync();
                answer.Data = true;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.Unauthorized;
            }

            return answer;
        }

        /// <summary>
        ///soru olusturuluyor
        /// </summary>
        /// <returns></returns>
        private QuestionResponse GenerateQuestion(RoomType roomType)
        {
            var response = new QuestionResponse
            {
                VariantResponses = new List<VariantResponse>()
            };
            /// <summary>
            ///easy,normal,hard,expert gore +,-,/,* kullanilarak sorular olusturuluyor
            /// </summary>
            /// <returns></returns>

            var r = new Random();
            var typeAlqoritm = r.Next(4);
            switch (roomType)
            {
                case RoomType.Kolay:
                    if (typeAlqoritm == 0)
                    {
                        var firstNumber = r.Next(21);
                        var secondNumber = r.Next(21);

                        response.Text = $"{firstNumber}+{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber + secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber + secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber + secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 1)
                    {
                        var firstNumber = r.Next(21);
                        var secondNumber = r.Next(21);

                        response.Text = $"{firstNumber}-{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber - secondNumber}";
                        while (true)
                        {
                            var text = $"{r.Next((firstNumber - secondNumber - 5), (firstNumber - secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 2)
                    {
                        var firstNumber = r.Next(1, 7);
                        var number = r.Next(1, 7);

                        response.Text = $"{firstNumber * number}÷{firstNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{number}";
                        while (true)
                        {
                            var text = $"{r.Next((number <= 5 ? 1 : number - 5), (number + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 3)
                    {
                        var firstNumber = r.Next(1, 11);
                        var secondNumber = r.Next(1, 11);

                        response.Text = $"{firstNumber}×{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber * secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber * secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber * secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }

                    return response;
                case RoomType.Normal:
                    if (typeAlqoritm == 0)
                    {
                        var firstNumber = r.Next(10, 51);
                        var secondNumber = r.Next(10, 51);

                        response.Text = $"{firstNumber}+{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber + secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber + secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber + secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 1)
                    {
                        var firstNumber = r.Next(10, 51);
                        var secondNumber = r.Next(10, 51);

                        response.Text = $"{firstNumber}-{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber - secondNumber}";
                        while (true)
                        {
                            var text = $"{r.Next((firstNumber - secondNumber - 5), (firstNumber - secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 2)
                    {
                        var firstNumber = r.Next(6, 10);
                        var number = r.Next(6, 10);

                        response.Text = $"{firstNumber * number}÷{firstNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{number}";
                        while (true)
                        {
                            var text = $"{r.Next((number <= 5 ? 1 : number - 5), (number + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 3)
                    {
                        var firstNumber = r.Next(5, 12);
                        var secondNumber = r.Next(5, 12);

                        response.Text = $"{firstNumber}×{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber * secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber * secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber * secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }

                    return response;
                case RoomType.Zor:
                    if (typeAlqoritm == 0)
                    {
                        var firstNumber = r.Next(30, 81);
                        var secondNumber = r.Next(30, 81);

                        response.Text = $"{firstNumber}+{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber + secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber + secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber + secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 1)
                    {
                        var firstNumber = r.Next(30, 81);
                        var secondNumber = r.Next(30, 81);

                        response.Text = $"{firstNumber}-{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber - secondNumber}";
                        while (true)
                        {
                            var text = $"{r.Next((firstNumber - secondNumber - 5), (firstNumber - secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 2)
                    {
                        var firstNumber = r.Next(9, 17);
                        var number = r.Next(9, 17);

                        response.Text = $"{firstNumber * number}÷{firstNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{number}";
                        while (true)
                        {
                            var text = $"{r.Next((number <= 5 ? 1 : number - 5), (number + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 3)
                    {
                        var firstNumber = r.Next(9, 18);
                        var secondNumber = r.Next(9, 18);

                        response.Text = $"{firstNumber}×{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber * secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber * secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber * secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }

                    return response;
                case RoomType.Uzman:
                    if (typeAlqoritm == 0)
                    {
                        var firstNumber = r.Next(50, 151);
                        var secondNumber = r.Next(50, 151);

                        response.Text = $"{firstNumber}+{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber + secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber + secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber + secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 1)
                    {
                        var firstNumber = r.Next(50, 151);
                        var secondNumber = r.Next(50, 151);

                        response.Text = $"{firstNumber}-{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber - secondNumber}";
                        while (true)
                        {
                            var text = $"{r.Next((firstNumber - secondNumber - 5), (firstNumber - secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 2)
                    {
                        var firstNumber = r.Next(12, 25);
                        var number = r.Next(12, 25);

                        response.Text = $"{firstNumber * number}÷{firstNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{number}";
                        while (true)
                        {
                            var text = $"{r.Next((number <= 5 ? 1 : number - 5), (number + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }
                    else if (typeAlqoritm == 3)
                    {
                        var firstNumber = r.Next(10, 26);
                        var secondNumber = r.Next(10, 26);

                        response.Text = $"{firstNumber}×{secondNumber}";
                        var isAnswerPosition = r.Next(4);
                        var answerText = $"{firstNumber * secondNumber}";
                        while (true)
                        {
                            var text =
                                $"{r.Next((firstNumber * secondNumber <= 5 ? 1 : firstNumber + secondNumber - 5), (firstNumber * secondNumber + 5))}";
                            if (response.VariantResponses.Count == isAnswerPosition)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = answerText,
                                    IsAnswer = true,
                                });
                            }
                            else if (response.VariantResponses.All(x => x.Text != text) && text != answerText)
                            {
                                response.VariantResponses.Add(new VariantResponse()
                                {
                                    Text = text,
                                    IsAnswer = false,
                                });
                            }

                            if (response.VariantResponses.Count == 4)
                                break;
                        }
                    }

                    return response;
            }

            return null;
        }

        /// <summary>
        ///levellerin hesaplanmasi
        /// </summary>
        /// <returns></returns>
        private (int level, string levelName) CalculateLevel(int score)
        {
            var level = 0;
            var levelName = string.Empty;
            switch (score)
            {
                case < 1500:
                    level = 0;
                    levelName = "Şapşal";
                    break;
                case < 2500:
                    level = 1;
                    levelName = "Kaldırım Mühendisi";
                    break;
                case < 3500:
                    level = 2;
                    levelName = "Tembel Reis";
                    break;
                case < 4500:
                    level = 3;
                    levelName = "Hırslı Tavşan";
                    break;
                case < 5500:
                    level = 4;
                    levelName = "Akıllı";
                    break;
                case < 6500:
                    level = 5;
                    levelName = "Zeki";
                    break;
                case < 7500:
                    level = 6;
                    levelName = "Hesap Makinesi";
                    break;
                case < 8500:
                    level = 7;
                    levelName = "Guru";
                    break;
                case < 9500:
                    level = 8;
                    levelName = "Şampiyon";
                    break;
                case < 10000000:
                    level = 9;
                    levelName = "Eniştein";
                    break;
                default:
                    level = 10;
                    levelName = "Insine";
                    break;
            }

            return (level, levelName);
        }

        /// <summary>
        ///kullanicini ismini ve resmini degistirmek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> InviteFriendToRoom(int friendId, int roomId)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var inviteFriend = await _db.InviteFriends.FirstOrDefaultAsync(x =>
                    x.UserId == userId && x.FriendId == friendId && x.RoomId == roomId);

                if (inviteFriend != null)
                {
                    answer.Data = false;
                    answer.Code = ResultCodes.Exist;
                }

                //var user = await _db.Users.FirstOrDefaultAsync(x=>x.Id==inviteFriend.FriendId);
                //if (user.UserPosition == UserPosition.Online)
                //{
                _db.InviteFriends.Add(new InviteFriend
                {
                    UserId = userId,
                    RoomId = roomId,
                    FriendId = friendId,
                    AddedDate = DateTime.Now.AddHours(-1),
                });
                await _db.SaveChangesAsync();
                answer.Data = true;
                answer.Code = ResultCodes.Ok;
                //}
                //else
                //{
                //    answer.Code = ResultCodes.UserIsNotOnline;
                //}
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.Unauthorized;
            }

            return answer;
        }


        /// <summary>
        ///kullanicini ismini ve resmini degistirmek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<List<InviteFriendResponse>>> InvitingFriends()
        {
            var answer = new AnswerResponse<List<InviteFriendResponse>>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                answer.Data = await _db.InviteFriends
                    .Where(x => x.FriendId == userId)
                    .Include(x => x.User)
                    .Select(x => new InviteFriendResponse
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        FriendId = x.FriendId,
                        RoomId = x.RoomId,
                        AddedDate = x.AddedDate,
                        ApprovedDate = x.ApprovedDate,
                        UserResponse = new UserResponse
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            Level = x.User.Level,
                            Email = x.User.Email,
                            Score = x.User.Score,
                            Photo = x.User.Photo
                        }
                    }).ToListAsync();

                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.Unauthorized;
            }

            return answer;
        }

        public async Task<AnswerResponse<List<AwaitedFriendResponse>>> AwaitedFriends(int count)
        {
            var answer = new AnswerResponse<List<AwaitedFriendResponse>>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                List<AwaitedFriendResponse> list = new List<AwaitedFriendResponse>();
                var awaitedFriendList = await _db.UserFriends
                    .Where(x => x.UserFriendStatus == UserFriendStatus.Request && x.FriendId == userId)
                    .Take(count)
                    .ToListAsync();
                foreach (var item in awaitedFriendList)
                {
                    var awaitedFriendInfo = await _db.Users
                        .Where(x => x.Id == item.UserId)
                        .Select(x => new AwaitedFriendResponse
                        {
                            UserId = x.Id,
                            UserName = x.UserName,
                            Photo = x.Photo
                        }).FirstOrDefaultAsync();
                    list.Add(awaitedFriendInfo);
                }

                answer.Data = list;
                answer.Code = ResultCodes.Ok;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.Unauthorized;
            }

            return answer;
        }

        /// <summary>
        ///kullanicini ismini ve resmini degistirmek
        /// </summary>
        /// <returns></returns>
        public async Task<AnswerResponse<bool>> CheckConfirmationCode(string code)
        {
            var answer = new AnswerResponse<bool>();
            try
            {

                var user1 = await _db.CheckEmails.FirstOrDefaultAsync(x => x.Code == code);
                var email = user1.Email;
                if (!(await _db.Users
                    .AnyAsync(x => x.Email.ToLower() == email.ToLower())))
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                if (!(await _db.CheckEmails.AnyAsync(x => x.Email.ToLower() == email.ToLower() && x.Code == code)))
                {

                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
                var userName = user.UserName;
                Random rnd = new Random();
                string password = "BrainX" + (rnd.Next(10000, 1000000)).ToString();
                var apiKey = "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";
                var t = "BrainX";
                var subject = ' ' + DateTime.Now.AddHours(-1).ToString("dd.MM.yyyy hh:mm:ss");
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("brainexperiencex@gmail.com", t);
                var to = new EmailAddress(email);
                var content = @$"<p><b>Yeni şifreniz: </b>{password}</p> <p><b>Kullanıcı isminiz: </b>{userName}</p>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, content, content);
                var response = await client.SendEmailAsync(msg);
                if (response.IsSuccessStatusCode)
                {

                    if (user != null)
                    {
                        password.CreatePasswordHash(out var passwordHash, out var passwordSalt);
                        user.PasswordHash = passwordHash;
                        user.PasswordSalt = passwordSalt;
                        await _db.SaveChangesAsync();
                    }
                }

                answer.Code = response.IsSuccessStatusCode ? ResultCodes.Ok : ResultCodes.ConfirmationCodeInvalid;
                answer.Data = true;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        public async Task<AnswerResponse<bool>> SendConfirmationCode(string email)
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                if (!(await _db.Users.AnyAsync(x => x.Email.ToLower() == email.ToLower())))
                {
                    answer.Code = ResultCodes.NotFound;
                    return answer;
                }

                Random rnd = new Random();
                string code = (rnd.Next(10000, 1000000)).ToString();
                var apiKey = "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";
                //var apiKey = "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";
                var t = "BrainX";
                var subject = ' ' + DateTime.Now.AddHours(-1).ToString("dd.MM.yyyy hh:mm:ss");
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("brainexperiencex@gmail.com", t);
                var to = new EmailAddress(email);
                var content = @$"
<p><b>Şifre yenileme için onay kodu: </b>{code}</p>
";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, content, content);
                var response = await client.SendEmailAsync(msg);
                if (response.IsSuccessStatusCode)
                {
                    _db.CheckEmails.Add(new CheckEmail()
                    {
                        Email = email,
                        Code = code,
                        AddedDate = DateTime.Now.AddHours(-1)
                    });
                    await _db.SaveChangesAsync();
                }


                answer.Code = response.IsSuccessStatusCode ? ResultCodes.Ok : ResultCodes.UnknownError;
                answer.Data = true;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.Unauthorized;
            }

            return answer;
        }

        public async Task<AnswerResponse<bool>> IncreaseLife()
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);

                if (user != null)
                {
                    if (user.Life < 10)
                    {
                        user.Life++;
                        await _db.SaveChangesAsync();
                    }
                }

                //
                answer.Code = ResultCodes.Ok;
                answer.Data = true;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }
        public async Task<AnswerResponse<bool>> IncreaseLifeRequestFriendToApp()
        {
            var answer = new AnswerResponse<bool>();
            try
            {
                var (userId, _) = GetUserId();

                if (userId == 0)
                {
                    answer.Code = ResultCodes.Unauthorized;
                    return answer;
                }

                var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);


                if (user != null)
                {
                    if (user.Life < 10 && user.Life + 5 < 10)
                    {
                        user.Life += 5;
                    }
                    else if (user.Life < 10)
                    {
                        user.Life = 10;
                    }
                }


                await _db.SaveChangesAsync();
                answer.Code = ResultCodes.Ok;
                answer.Data = true;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }

        public async Task<AnswerResponse<bool>> IncreaseUserLifeForTimer()
        {
            var answer = new AnswerResponse<bool>();

            try
            {
                var users = await _db.Users.ToListAsync();
                foreach (var user in users)
                {
                    _db.Entry(user).Reload();
                    //if (user.Id == 27)
                    //{
                    //    int a = 47;
                    //}
                    if (user.Life < 10)
                    {
                        int count = 0;
                        var User = user;
                        var currentDate = DateTime.Now.AddHours(-1);
                        var restOfLifeDate = user.LastGameTime;
                        TimeSpan difference = currentDate - restOfLifeDate;
                        double remainMinute = Convert.ToDouble(difference.Minutes);
                        double remainSecond = Convert.ToDouble(difference.Seconds);
                        string time = String.Format("{0}.{1}", remainMinute, remainSecond);
                        User.LifeTime = (User.LifeTime - Convert.ToDouble(time));
                        count = Convert.ToInt32(remainMinute) / oneLifeTime;
                        for (int i = 0; i < count; i++)
                        {
                            if (User.Life < 10)
                            {
                                User.Life++;
                            }
                        }

                        if (((10 - User.Life) * oneLifeTime - oneLifeTime >= (((int)User.LifeTime))) &&
                            user.Life < 10 && count == 0)
                        {
                            User.Life++;
                        }

                        User.LastGameTime = currentDate;
                        await _db.SaveChangesAsync();
                    }
                }

                answer.Code = ResultCodes.Ok;
                answer.Data = true;
            }
            catch (Exception e)
            {
                answer.Code = ResultCodes.UnknownError;
            }

            return answer;
        }
    }
}