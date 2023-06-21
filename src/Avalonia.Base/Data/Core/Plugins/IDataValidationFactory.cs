namespace Avalonia.Data.Core.Plugins;

internal interface IDataValidationFactory
{
    public IPropertyAccessor? TryCreate(object? reference, string propertyName, IPropertyAccessor accessor);
}
