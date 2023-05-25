using Microsoft.EntityFrameworkCore;
using mstg.Entities;

namespace mstg.Data
{
    public class Users
    {
        public async static Task<Entities.User> GetUser(Entities.User user)
        {
            //user = new User();

            using (var db = new Database())
            {
                var existingUser = await db
                    .Users
                    .Where(u => u.Service == user.Service)
                    .Where(u => u.ServiceId == user.ServiceId)
                    .FirstOrDefaultAsync();

                if (existingUser == null)
                {
                    var newUser = new User
                    {
                        LastAccessedAt = DateTime.UtcNow,
                        Service = user.Service,
                        ServiceId = user.ServiceId,
                        ServiceName = user.ServiceName,
                        ServiceUsername = user.ServiceUsername
                    };

                    newUser.Id = await GenerateId(newUser);

                    user = newUser;
                    await db.Users.AddAsync(newUser);
                }
                else
                {
                    existingUser.LastAccessedAt = DateTime.UtcNow;
                    existingUser.ServiceName = user.ServiceName;
                    existingUser.ServiceUsername = user.ServiceUsername;

                    user = existingUser;
                }

                await db.SaveChangesAsync();
            }

            return user;
        }

        async static Task<string> GenerateId(User user)
        {
            string serviceString = user.Service.ToString().ToLower();
            return $"{serviceString}_{user.ServiceId}";
        }
    }
}