using Microsoft.EntityFrameworkCore;
using Block_Chain_Example_1.Models;

namespace Block_Chain_Example_1.Data
{
    public class BlockChainDbContext : DbContext
    {
        public BlockChainDbContext(DbContextOptions<BlockChainDbContext> options)
                    : base(options)
        {
        }

        public DbSet<Block> Blocks { get; set; }
    }
}
