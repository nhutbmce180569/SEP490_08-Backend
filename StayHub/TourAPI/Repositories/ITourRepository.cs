﻿using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourRepository
    {
        Task Add(Tour model);
        Task<List<Tour>> GetAll();
        Task<List<Tour>> GetActiveTours();
        Task<Tour> GetById(int id);
        void Update(Tour model);
        Task Delete(int id);
        Task SaveChangesAsync();
        Task<int> CountByCategoryIdAsync(int categoryId);
    }
}
