using System.ComponentModel.DataAnnotations;

namespace Application.RequestFeatures;

public class ProjectQueryParameters
{
    public string? SearchTerm { get; set; } // поиск по названию/описанию
    public DateTime? FromDate { get; set; } // задачи, созданные после этой даты
    public DateTime? ToDate {  get; set; }// задачи, созданные до этой даты

    // Сортировка
    private static readonly string[] AllowedSortFields = { "name", "createdat" };
    private string? sortBy;

    public string? SortBy
    {
        get => sortBy;
        set => sortBy = AllowedSortFields.Contains(value?.ToLower()) ? value.ToLower() : null;
    }

    [RegularExpression("^(asc|desc)$", ErrorMessage = "Параметр SortOrder должен быть 'asc' или 'desc'")]
    public string? SortOrder { get; set; } = "asc";

    // Пагинация
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set=> _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}
