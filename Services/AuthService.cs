using MongoDB.Driver;
using MongoDB.Bson;
using RealEstate.API.Models;
using RealEstate.API.Dtos;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using BCrypt.Net;

namespace RealEstate.API.Services
{
    public class AuthService
    {
        private readonly IMongoCollection<User> _users;

        public AuthService(IConfiguration config)
        {
            var connectionString = config["MONGO_CONNECTION"] 
                                   ?? throw new Exception("MONGO_CONNECTION no definida");
            var databaseName = config["MONGO_DATABASE"] 
                               ?? throw new Exception("MONGO_DATABASE no definida");
            var collectionName = config["MONGO_COLLECTION_USERS"] 
                                 ?? throw new Exception("MONGO_COLLECTION_USERS no definida");

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _users = database.GetCollection<User>(collectionName);

            // Opcional: índice por email para login rápido
            _users.Indexes.CreateOne(
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(u => u.Email),
                                           new CreateIndexOptions { Unique = true })
            );
        }

        // ===========================================================
        // 🔹 REGISTRAR USUARIO
        // ===========================================================
        public async Task<User> RegisterAsync(User user)
        {
            // Verificar si existe
            var exists = await _users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();
            if (exists != null)
                throw new Exception("Email ya registrado");

            // Hashear contraseña
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _users.InsertOneAsync(user);
            return user;
        }

        // ===========================================================
        // 🔹 LOGIN
        // ===========================================================
        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null) return null;

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                return null;

            return user;
        }

        // ===========================================================
        // 🔹 ACTUALIZAR CONTRASEÑA
        // ===========================================================
        public async Task<bool> UpdatePasswordAsync(string userId, string newPassword)
        {
            if (!ObjectId.TryParse(userId, out _))
                throw new Exception($"El id '{userId}' no es válido.");

            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return false;

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var update = Builders<User>.Update.Set(u => u.Password, hashed);
            var result = await _users.UpdateOneAsync(u => u.Id == userId, update);

            return result.ModifiedCount > 0;
        }

        // ===========================================================
        // 🔹 OBTENER USUARIO POR ID
        // ===========================================================
        public async Task<User?> GetByIdAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out _)) return null;
            return await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        }

        // ===========================================================
        // 🔹 VERIFICAR SI USUARIO EXISTE
        // ===========================================================
        public async Task<bool> ExistsAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out _)) return false;
            var count = await _users.CountDocumentsAsync(u => u.Id == userId);
            return count > 0;
        }
    }
}
