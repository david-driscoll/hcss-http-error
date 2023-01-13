var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddErrorFilter<MyErrorFilter>();

var app = builder.Build();

app.MapGraphQL();

app.Run();


public class Book
{
    public string Title { get; set; }

    public Author Author { get; set; }
}

public class Author
{
    public string Name { get; set; }
}


public class Query
{
    public Book GetBook() => throw new NotImplementedException();
    public Author GetAuthor() => new Author() { Name = "test" };
}

public class MyErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error is { Exception: NotImplementedException })
        {
            return error.WithMessage("This is a custom error message.");
        }

        return error;
    }
}
