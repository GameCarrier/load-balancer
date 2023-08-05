using System;

namespace LoadBalancer.Common
{
    [ReflectionSerialization]
    public class Result
    {
        [KeyType((byte)CommonParameters.Status)] public KeyType Status { get; set; } = KeyType.Empty;
        [KeyType((byte)CommonParameters.Message)] public string Message { get; set; }
        [KeyType((byte)CommonParameters.StatusName)] public string StatusName { get; set; }
        [AvoidSerialization] public bool IsOk => Status == KeyType.Empty;

        public static Result Ok(string message = null) => new Result().Ok(message);
        public static Result Error(Enum status, string message = null) => new Result().Error(status, message);
        public static Result Error(KeyType status, string message = null) => new Result().Error(status, message);

        public override string ToString() => $"{Status} {StatusName} '{Message}'";
    }

    public static class ResultExtensions
    {
        public static T Ok<T>(this T result, string message = null) where T : Result
        {
            result.StatusName = "Ok";
            result.Message = message;
            return result;
        }

        public static T Error<T>(this T result, Enum status, string message = null) where T : Result
        {
            result.Status = status;
            result.StatusName = status.ToString();
            result.Message = message;
            return result;
        }

        public static T Error<T>(this T result, KeyType status, string message = null) where T : Result
        {
            result.Status = status;
            result.StatusName = status.ToString();
            result.Message = message;
            return result;
        }
    }
}
