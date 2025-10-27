using System.Text.Json.Serialization;

namespace RealEstate.API.Infraestructure.Core.Services
{
    /// <summary>
    /// Wrapper genérico de resultados de servicio.
    /// Estandariza respuestas internas en todos los módulos (User, Owner, Property, etc.).
    /// Ideal para retornar desde servicios hacia controladores.
    /// </summary>
    public class ServiceResultWrapper<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; private set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; private set; }

        [JsonPropertyName("message")]
        public string Message { get; private set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; private set; }

        [JsonPropertyName("errors")]
        public IReadOnlyList<string>? Errors { get; private set; }

        private ServiceResultWrapper() { }

        // ===========================================================
        // Éxitos
        // ===========================================================
        public static ServiceResultWrapper<T> Ok(T? data, string message = "Operación exitosa")
            => new()
            {
                Success = true,
                StatusCode = 200,
                Data = data,
                Message = message
            };

        public static ServiceResultWrapper<T> Created(T? data, string message = "Recurso creado correctamente")
            => new()
            {
                Success = true,
                StatusCode = 201,
                Data = data,
                Message = message
            };

        public static ServiceResultWrapper<T> Updated(T? data, string message = "Recurso actualizado correctamente")
            => new()
            {
                Success = true,
                StatusCode = 200,
                Data = data,
                Message = message
            };

        public static ServiceResultWrapper<T> Deleted(string message = "Recurso eliminado correctamente")
            => new()
            {
                Success = true,
                StatusCode = 200,
                Message = message
            };

        // ===========================================================
        // Fallos controlados
        // ===========================================================
        public static ServiceResultWrapper<T> Fail(string message, int statusCode = 400)
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = new List<string> { message }
            };

        public static ServiceResultWrapper<T> Fail(IEnumerable<string> errors, int statusCode = 400, string message = "Errores detectados en la operación")
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = errors.ToList()
            };

        // ===========================================================
        // Excepciones no controladas
        // ===========================================================
        public static ServiceResultWrapper<T> Error(Exception ex, int statusCode = 500)
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = "Ocurrió un error inesperado en el servicio",
                Errors = new List<string> { ex.Message }
            };

        public static ServiceResultWrapper<T> Error(string message, int statusCode = 500)
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = new List<string> { message }
            };
    }
}
