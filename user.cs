using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public int PageCount { get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Book> Books { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=testdb;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}

public static class SecurityHelper
{
  public static bool ValidatePassword(string enteredPassword, string hashedPassword, string salt)
  {
    string enteredPasswordHash = HashPassword(enteredPassword, salt);

    return enteredPasswordHash == hashedPassword;
  }

  public static string HashPassword(string password, string salt)
  {
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
      byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
      return Convert.ToBase64String(hashedBytes);
    }
  }
}

public class UserRepository
{
    private readonly ApplicationContext _context;

    public UserRepository(ApplicationContext context)
    {
        _context = context;
    }

  public bool AuthenticateUser(string userName, string password)
  {
    var user = _context.Users.SingleOrDefault(u => u.UserName == userName);

    if (user == null)
    {
      Console.WriteLine("User not found.");
      return false;
    }

    if (user.IsBlocked)
    {
      Console.WriteLine("Account is blocked. Contact support for assistance.");
      return false;
    }

    if (SecurityHelper.ValidatePassword(password, user.Password, user.Salt))
    {
      invalidLoginAttempts = 0;
      Console.WriteLine("Login successful!");
      return true;
    }
    else
    {
      Console.WriteLine("Invalid password.");

      invalidLoginAttempts++;

      if (invalidLoginAttempts >= 3)
      {
        Console.WriteLine("Too many invalid login attempts. Your account is blocked.");
        user.IsBlocked = true;
        _context.SaveChanges();
      }

      return false;
    }
  }


  public void BlockUser(string userName)
  {
    var user = _context.Users.SingleOrDefault(u => u.UserName == userName);

    if (user != null)
    {
      user.IsBlocked = true;
      _context.SaveChanges();
      Console.WriteLine($"User '{userName}' has been blocked.");
    }
    else
    {
      Console.WriteLine($"User '{userName}' not found. Unable to block.");
    }
  }
}

public class BookRepository
{
    private int pageSize = 5;
    private readonly ApplicationContext _context;

    public BookRepository(ApplicationContext context)
    {
        _context = context;
    }

    public void EnsurePopulate()
    {
        if (!_context.Books.Any())
        {
            _context.Books.AddRange(Book.TestData());
            _context.SaveChanges();
        }
    }

    public IEnumerable<Book> GetBooks(int page = 1)
    {
        return _context.Books.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
    }

    public Book GetBookById(int id)
    {
        return _context.Books.FirstOrDefault(book => book.Id == id);
    }
}

public static class Program
{
    private static int invalidLoginAttempts = 0;

    public static void Main()
    {
        using (var db = new ApplicationContext())
        {
            var userRepository = new UserRepository(db);
            var bookRepository = new BookRepository(db);

            userRepository.AuthenticateUser("username", "password"); 

            while (true)
            {
                Console.WriteLine("1. Show all Books\n2. Get Book by Id\n3. Logout");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        var allBooks = bookRepository.GetBooks();
                        DisplayTable(allBooks);
                        break;

                    case "2":
                        Console.Write("Enter Book Id: ");
                        if (int.TryParse(Console.ReadLine(), out int bookId))
                        {
                            var book = bookRepository.GetBookById(bookId);
                            if (book != null)
                            {
                                DisplayTable(new List<Book> { book });
                            }
                            else
                            {
                                Console.WriteLine("Book not found.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid input for Book Id.");
                        }
                        break;

                    case "3":
                        return;

                    default:
                        Console.WriteLine("Not a valid input!");
                        break;
                }
        if (userRepository.AuthenticateUser("username", "password"))
        {
          invalidLoginAttempts = 0;
        }
        else
        {
          invalidLoginAttempts++;

          if (invalidLoginAttempts >= 3)
          {
            Console.WriteLine("Too many invalid login attempts. Your account is blocked.");

            userRepository.BlockUser("username");

            return;
          }
          else
          {
            Console.WriteLine("Invalid username or password.");
          }
        }

      }
    }
    }

  private static void DisplayTable<T>(List<T> collection, string[] excludeProperties = null)
  {
    if (collection == null || collection.Count == 0)
    {
      Console.WriteLine("No data to display.");
      return;
    }

    var properties = typeof(T).GetProperties()
        .Where(prop => excludeProperties == null || !excludeProperties.Contains(prop.Name))
        .ToArray();

    foreach (var property in properties)
    {
      Console.Write($"{property.Name,-15} | ");
    }

    Console.WriteLine(); 

    foreach (var item in collection)
    {
      foreach (var property in properties)
      {
        var value = property.GetValue(item);
        Console.Write($"{value,-15} | ");
      }

      Console.WriteLine(); 
    }
  }
}
