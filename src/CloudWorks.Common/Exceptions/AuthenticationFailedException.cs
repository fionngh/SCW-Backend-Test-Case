namespace Common.Exceptions;

public sealed class AuthenticationFailedException(string message) : Exception(message);
