using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MathematicGameApi.Infrastructure.Containers.Requests;
using MathematicGameApi.Infrastructure.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathematicGameApi.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ICoreService _coreService;

        public HomeController(ICoreService coreService)
        {
            _coreService = coreService;
        }

        [HttpGet("getRooms")]
        //[Authorize]
        public async Task<IActionResult> GetRooms(int page, int count)
        {
            var result = await _coreService.GetRooms(page, count);

            return (result == null) ? NotFound("Rooms") : Ok(result);
        }

        [HttpGet("getRoomUsers")]
        [Authorize]
        public async Task<IActionResult> GetRoomDetails([FromQuery] RoomUsersDto request)
        {
            var result = await _coreService.GetRoomDetails(request.RoomId);

            return Ok(result);
        }

        [HttpGet("getUserFriends")]
        // [Authorize]
        public async Task<IActionResult> GetUserFriends(int page, int count)
        {
            var result = await _coreService.GetUserFriends(page, count);

            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("getUserFriendsRoom")]
        // [Authorize]
        public async Task<IActionResult> GetUserFriendsRoom(int page, int count)
        {
            var result = await _coreService.GetUserFriendsRoom(page, count);

            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("createRoom")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto request)
        {
            var result = await _coreService.CreateRoom(request.UserCount, request.Type);

            return Ok(result);
        }

        [HttpPost("againPlay")]
        [Authorize]
        public async Task<IActionResult> AgainPlay([FromBody] AgainPlayDto request)
        {
            var result = await _coreService.AgainPlay(request.UserCount, request.Type, request.AgainKey);

            return Ok(result);
        }

        [HttpGet("getUserInfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var result = await _coreService.GetUserInfo();

            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("searchUser")]
        [Authorize]
        public async Task<IActionResult> SearchUser([FromQuery] SearchUserDto request)
        {
            var result = await _coreService.SearchUsers(request.UserName, request.Page);

            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("topUsers")]
        [Authorize]
        public async Task<IActionResult> TopUsers([FromQuery] TopUsersDto request)
        {
            var result = await _coreService.TopUsers(request.TopCount);

            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("requestFriend")]
        [Authorize]
        public async Task<IActionResult> RequestFriend([FromQuery] RequestFriendDto request)
        {
            var result = await _coreService.RequestFriend(request.Id);

            return Ok(result);
        }

        [HttpPost("approveOrCancelFriendRequest")]
        [Authorize]
        public async Task<IActionResult> ApproveOrCancelFriendRequest(
            [FromQuery] ApproveOrCancelFriendRequestDto request)
        {
            var result = await _coreService.ApproveOrCancelFriendRequest(request.Id, request.Status);

            return Ok(result);
        }
        [HttpPost("checkPosition")]
        [Authorize]
        public async Task<IActionResult> CheckPosition(
            [FromQuery] CheckPositionDto request)
        {
            var result = await _coreService.CheckPosition(request.Id, request.Status);

            return Ok(result);
        }

        
        [HttpPost("addUserPosition")]
        [Authorize]
        public async Task<IActionResult> AddUserPosition(
            [FromBody] AddUserPositionDto request)
        {
            var result = await _coreService.AddUserPosition(request.Status);

            return Ok(result);
        }
        [HttpGet("getAwaitedFriends")]
        [Authorize]
        public async Task<IActionResult> AwaitedFriends([FromQuery] AwaitedFriendDto request)
        {
            var result = await _coreService.AwaitedFriends(request.Count);

            return Ok(result);
        }

        [HttpPost("addUserToRoom")]
        [Authorize]
        public async Task<IActionResult> AddUserToRoom([FromQuery] AddUserToRoomDto request)
        {
            var result = await _coreService.AddUserToRoom(request.RoomId);

            return Ok(result);
        }

        [HttpPost("deleteUserToRoom")]
        [Authorize]
        public async Task<IActionResult> DeleteUserFromRoom([FromQuery] DeleteUserToRoomDto request)
        {
            var result = await _coreService.DeleteUserFromRoom(request.RoomId);

            return Ok(result);
        }

        [HttpPost("startedGameByRoomId")]
        [Authorize]
        public async Task<IActionResult> StartedGameByRoomId([FromQuery] StartedGameByRoomId request)
        {
            var result = await _coreService.StartedGameByRoomId(request.RoomId);

            return Ok(result);
        }

        [HttpGet("getNextQuestion")]
        [Authorize]
        public async Task<IActionResult> GetNextQuestion([FromQuery] NextQuestionDto request)
        {
            var result = await _coreService.GetNextQuestion(request.VariantId, request.RoomId, request.NextQuestion);

            return Ok(result);
        }

        [HttpPost("endedGameByRoomId")]
        [Authorize]
        public async Task<IActionResult> EndedGameByRoomId([FromQuery] EndedGameByRoomId request)
        {
            var result = await _coreService.EndedGameByRoomId(request.RoomId);

            return Ok(result);
        }

        [HttpPost("increaseLife")]
        [Authorize]
        public async Task<IActionResult> IncreaseLife()
        {
            var result = await _coreService.IncreaseLife();

            return Ok(result);
        }
        [HttpPost("increaseLifeRequestFriendToApp")]
        [Authorize]
        public async Task<IActionResult> IncreaseLifeRequestFriendToApp()
        {
            var result = await _coreService.IncreaseLifeRequestFriendToApp();

            return Ok(result);
        }

        [HttpPost("inviteFriendToRoom")]
        [Authorize]
        public async Task<IActionResult> InviteFriendToRoom([FromQuery] InviteFriendToRoom request)
        {
            var result = await _coreService.InviteFriendToRoom(request.FriendId, request.RoomId);

            return Ok(result);
        }

        [HttpGet("getInviteFriends")]
        [Authorize]
        public async Task<IActionResult> InvitingFriends()
        {
            var result = await _coreService.InvitingFriends();

            return Ok(result);
        }

        [HttpPost("approveOrCancelPlayRequest")]
        [Authorize]
        public async Task<IActionResult> ApproveOrCancelPlayRequest([FromQuery] ApproveOrCancelPlayRequestDto request)
        {
            var result = await _coreService.ApproveOrCancelPlayRequest(request.Id, request.Status);

            return Ok(result);
        }


        [HttpGet("images/{name}")]
        public IActionResult Image(string name)
        {
            if (name == null)
            {
                return NotFound();
            }
            else
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "Images", name);

                if (!System.IO.File.Exists(fullPath))
                    return NotFound();

                return PhysicalFile(fullPath, "image/jpeg");
            }
        }
        [HttpGet("audio/{name}")]
        public IActionResult Audio(string name)
        {
            if (name == null)
            {
                return NotFound();
            }
            else
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "Videos", name);

                if (!System.IO.File.Exists(fullPath))
                    return NotFound();

                return PhysicalFile(fullPath, "audio/mpeg");
            }
        }
    }
}