using System.Data;
using FoodOrder.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodOrder.Helpers;

public static class SqlProc
{
    public static async Task ExecuteAsync(FoodOrderContext db, string procedureName, params (string Name, object? Value)[] parameters)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync();
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }
}
