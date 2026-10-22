namespace Domain.Access.Enums;

public enum AccessFailureReason
{
    UnknownUser = 1,
    UserInactive = 2,
    DoorInactive = 3,
    NoPermission = 4,
    DeviceError = 5,
    DeviceTimeout = 6,
}
