namespace DemoBank.Core.DTOs;

public class ResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; } = new List<string>();

    public static ResponseDto<T> SuccessResponse(T data, string message = "Success")
    {
        return new ResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ResponseDto<T> ErrorResponse(string message, List<string> errors = null)
    {
        return new ResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}