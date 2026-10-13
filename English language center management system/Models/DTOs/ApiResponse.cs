using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(bool success, string message, T data = default(T))
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public static ApiResponse<T> OK(T data, string message = "Thao tác thành công!")
        {
            return new ApiResponse<T> { Success = true, Message = message, Data = data };
        }

        public static ApiResponse<T> Fail(string message, T data = default(T))
        {
            return new ApiResponse<T> { Success = false, Message = message, Data = data };
        }
    }
}
