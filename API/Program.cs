using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Application.Common.Interfaces;
using Application.Features.Users.Commands;
using Domain.Entities;


namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IApplicationDBContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

           

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


                // seed: make a default school if there isnt any
                if (!db.Schools.Any())
                {
                    db.Schools.Add(new School
                    {
                        SchoolId = Guid.NewGuid(),
                        Name = "Default School",
                        Timezone = "Europe/Stockholm"
                    });

                    db.SaveChanges();
                }
            }


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
