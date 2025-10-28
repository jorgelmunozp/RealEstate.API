using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RealEstate.API.Infraestructure.Core.Services
{
    public class ServiceResultWrapper<T>
    {
        [JsonPropertyName("success")] public bool Success { get; }
        [JsonPropertyName("statusCode")] public int StatusCode { get; }
        [JsonPropertyName("message")] public string Message { get; }
        [JsonPropertyName("data")] public T? Data { get; }
        [JsonPropertyName("errors")] public IReadOnlyList<string>? Errors { get; }

        // usa nombres de parámetros en camelCase y asigna a las propiedades
        public ServiceResultWrapper(bool success, int statusCode, T? data = default, string message = "", IReadOnlyList<string>? errors = null)
        {
            Success = success;
            StatusCode = statusCode;
            Data = data;
            Message = message;
            Errors = errors ?? new List<string>();
        }

        public static ServiceResultWrapper<T> Ok(T? data, string message = "Operación exitosa")
            => new(true, 200, data, message);

        public static ServiceResultWrapper<T> Created(T? data, string message = "Recurso creado correctamente")
            => new(true, 201, data, message);

        public static ServiceResultWrapper<T> Updated(T? data, string message = "Recurso actualizado correctamente")
            => new(true, 200, data, message);

        public static ServiceResultWrapper<T> Deleted(string message = "Recurso eliminado correctamente")
            => new(true, 200, default, message);

        public static ServiceResultWrapper<T> Fail(string message, int statusCode = 400)
            => new(false, statusCode, default, message, new List<string> { message });

        public static ServiceResultWrapper<T> Fail(IEnumerable<string> errors, int statusCode = 400, string message = "Errores detectados en la operación")
            => new(false, statusCode, default, message, errors.ToList());

        public static ServiceResultWrapper<T> Error(Exception ex, int statusCode = 500)
            => new(false, statusCode, default, "Ocurrió un error inesperado en el servicio", new List<string> { ex.Message });

        public static ServiceResultWrapper<T> Error(string message, int statusCode = 500)
            => new(false, statusCode, default, message, new List<string> { message });
    }
}
