

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HelpDesk.Infrastructure.Persistence;

public class HelpDeskDbContextFactory : IDesignTimeDbContextFactory<HelpDeskDbContext>
{
    public HelpDeskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HelpDeskDbContext>();
        var connectionString = "Server=localhost;User ID=root;Password=React@26;Database=HelpDeskDb";

        optionsBuilder.UseMySql(connectionString, MySqlServerVersion.AutoDetect(connectionString));

        return new HelpDeskDbContext(optionsBuilder.Options);
    }
}