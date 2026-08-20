using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Kernel.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public int StatusCode { get; private set; }
        public string Message { get; private set; }

        private Result(bool isSuccess, T data, int statusCode, string message)
        {
            IsSuccess = isSuccess;
            Data = data;
            StatusCode = statusCode;
            Message = message;
        }

        public static Result<T> Success(T data, int statusCode = 200, string message = "")
        {
            return new Result<T>(true, data, statusCode, message);
        }

        public static Result<T> Failure(int statusCode = 400, string message = "")
        {
            // Add '!' to default to silence the nullability warning
            return new Result<T>(false, default!, statusCode, message);
        }

        public static Result<T> Failure(T data, int statusCode = 400, string message = "")
        {
            return new Result<T>(false, data, statusCode, message);
        }
    }
}
