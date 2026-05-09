using HttpServerApp.Models;

namespace HttpServerApp.Repositories;

public class CatRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<int, Cat> _cats = new();
    private int _nextId = 1;

    public CatRepository()
    {
        Add(new Cat { Name = "Luna", Breed = "Siberian", Age = 3 });
        Add(new Cat { Name = "Milo", Breed = "British Shorthair", Age = 5 });
        Add(new Cat { Name = "Nala", Breed = "Maine Coon", Age = 2 });
    }

    public IReadOnlyList<Cat> GetAll()
    {
        lock (_lock)
            return _cats.Values.OrderBy(c => c.Id).ToList();
    }

    public Cat? GetById(int id)
    {
        lock (_lock)
            return _cats.TryGetValue(id, out var cat) ? cat : null;
    }

    public Cat Add(Cat cat)
    {
        lock (_lock)
        {
            cat.Id = _nextId++;
            _cats[cat.Id] = cat;
            return cat;
        }
    }

    public Cat? Update(int id, Cat updated)
    {
        lock (_lock)
        {
            if (!_cats.TryGetValue(id, out var existing))
                return null;

            existing.Name = updated.Name;
            existing.Breed = updated.Breed;
            existing.Age = updated.Age;
            return existing;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
            return _cats.Remove(id);
    }
}
