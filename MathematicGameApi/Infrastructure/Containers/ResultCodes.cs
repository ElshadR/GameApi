using System.ComponentModel;
using System.Runtime.Serialization;

namespace MathematicGameApi.Infrastructure.Containers
{
    public enum ResultCodes
    {
        [Description("Ok")]
        [EnumMember(Value = "Ok")]
        Ok,
        [Description("NotFound")]
        [EnumMember(Value = "NotFound")]
        NotFound,
        [Description("UnknownError")]
        [EnumMember(Value = "UnknownError")]
        UnknownError,
        [Description("PasswordInvalid")]
        [EnumMember(Value = "PasswordInvalid")]
        PasswordInvalid,
        [Description("Unauthorized")]
        [EnumMember(Value = "Unauthorized")]
        Unauthorized,
        [Description("Exist")]
        [EnumMember(Value = "Exist")]
        Exist,
        [Description("RoomIsFull")]
        [EnumMember(Value = "RoomIsFull")]
        RoomIsFull,
        [Description("PlayInviteAccept")]
        [EnumMember(Value = "PlayInviteAccept")]
        PlayInviteAccept,
        [Description("PlayInviteReject")]
        [EnumMember(Value = "PlayInviteReject")]
        PlayInviteReject,
        [Description("ConfirmationCodeInvalid")]
        [EnumMember(Value = "ConfirmationCodeInvalid")]
        ConfirmationCodeInvalid,
        [Description("UserIsNotOnline")]
        [EnumMember(Value = "UserIsNotOnline")]
        UserIsNotOnline

    }
}