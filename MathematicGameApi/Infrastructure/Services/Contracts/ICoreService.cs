using System.Collections.Generic;
using System.Threading.Tasks;
using MathematicGameApi.Infrastructure.Containers;
using MathematicGameApi.Infrastructure.Containers.Requests;
using MathematicGameApi.Infrastructure.Containers.Responses;
using MathematicGameApi.Infrastructure.Enums;
using Microsoft.AspNetCore.Http;

namespace MathematicGameApi.Infrastructure.Services.Contracts
{
    public interface ICoreService
    {
        Task<bool> CheckUserName(string userName);
        Task<AnswerResponse<RegisterResponse>> Register(string userName, string email, string password);
        Task<AnswerResponse<LoginResponse>> Login(string userName, string password);
        Task<AnswerResponse<List<RoomResponse>>> GetRooms(int page, int count);
        Task<AnswerResponse<RoomResponse>> GetRoomDetails(int roomId);
        Task<AnswerResponse<List<UserResponse>>> GetUserFriends(int page, int count);
        Task<AnswerResponse<List<UserResponse>>> GetUserFriendsRoom(int page, int count);
        Task<AnswerResponse<RoomResponse>> CreateRoom(int userCount, RoomType type);
        Task<AnswerResponse<int>> AgainPlay(int userCount, RoomType type, string againKey);
        Task<AnswerResponse<UserResponse>> GetUserInfo();
        Task <AnswerResponse<List<UserResponse>>> SearchUsers(string userName, int page);
        Task<AnswerResponse<List<TopUserResponse>>> TopUsers(int count);
        Task<AnswerResponse<bool>> RequestFriend(int id);
        Task<AnswerResponse<bool>> ApproveOrCancelFriendRequest(int id, UserFriendStatus status);
        Task<AnswerResponse<bool>> CheckPosition(int id, UserPosition status);
        Task<AnswerResponse<bool>> AddUserPosition(UserPosition position);
        Task<AnswerResponse<bool>> AddUserToRoom(int roomId);
        Task<AnswerResponse<bool>> DeleteUserFromRoom(int roomId);
        Task<AnswerResponse<bool>> StartedGameByRoomId(int roomId);
        Task<AnswerResponse<QuestionResponse>> GetNextQuestion(int variantId, int roomId, int iQuestion);
        Task<AnswerResponse<EndedGameResponse>> EndedGameByRoomId(int roomId);
        Task<AnswerResponse<bool>> UpdateUser(int id, string userName, IFormFile photo);
        Task<AnswerResponse<bool>> InviteFriendToRoom(int friendId, int roomId);
        Task<AnswerResponse<List<InviteFriendResponse>>> InvitingFriends();
        Task<AnswerResponse<List<AwaitedFriendResponse>>> AwaitedFriends(int count);
        Task<AnswerResponse<bool>> SendConfirmationCode(string email);
        Task<AnswerResponse<bool>> CheckConfirmationCode(string code);
        Task<AnswerResponse<bool>> IncreaseLife();
        Task<AnswerResponse<bool>> IncreaseLifeRequestFriendToApp();
        Task<AnswerResponse<bool>> ApproveOrCancelPlayRequest(int id, UserPlayRequest status);
        Task<AnswerResponse<bool>> IncreaseUserLifeForTimer();

    }
}