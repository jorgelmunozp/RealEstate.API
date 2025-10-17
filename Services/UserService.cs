using MongoDB.Driver;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(IMongoDatabase database)
    {
        // var collectionName = config["MONGO_COLLECTION_USERS"]
        //              ?? throw new Exception("MONGO_COLLECTION_USERS no definida");

        // _users = database.GetCollection<User>(collectionName);

        _users = database.GetCollection<User>("users");

    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        await _users.InsertOneAsync(user);
        return user;
    }
}
