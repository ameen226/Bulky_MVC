using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Product obj)
        {
            Product product = _db.Products.FirstOrDefault(p => p.Id == obj.Id);
            
            if (product != null)
            {
                product.Title = obj.Title;
                product.Description = obj.Description;
                product.ISBN = obj.ISBN;
                product.Price = obj.Price;
                product.Price50 = obj.Price50;
                product.ListPrice = obj.ListPrice;
                product.Price100 = obj.Price100;
                product.CategoryId = obj.CategoryId;
                product.Author = obj.Author;
                product.ProductImages = obj.ProductImages;
                //if (obj.ImageUrl != null)
                //{
                //    product.ImageUrl = obj.ImageUrl;
                //}
            }
        }
    }
}
