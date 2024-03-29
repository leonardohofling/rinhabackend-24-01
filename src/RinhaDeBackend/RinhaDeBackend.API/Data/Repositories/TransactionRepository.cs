﻿using Npgsql;
using RinhaDeBackend.API.Data.Models;
using System.Data;

namespace RinhaDeBackend.API.Data.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly DiagnosticsConfig _diagnosticsConfig;

        #region SQL Commands

        private readonly NpgsqlCommand getTransactionsCommand =
            new NpgsqlCommand($"SELECT transaction_id, transaction_amount, transaction_type, transaction_description, created_at FROM transactions where customer_id = $1 ORDER BY transaction_id DESC LIMIT $2")
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer },
                    new NpgsqlParameter<int> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer }
                }
            };
        private readonly NpgsqlCommand insertCommand =
            new NpgsqlCommand("INSERT INTO transactions (customer_id, transaction_amount, transaction_type, transaction_description) values($1, $2, $3, $4)")
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer },
                    new NpgsqlParameter<int> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Integer },
                    new NpgsqlParameter<string> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar },
                    new NpgsqlParameter<string> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar }
                }
            };

        #endregion

        public TransactionRepository(IConnectionFactory connectionFactory, DiagnosticsConfig diagnosticsConfig)
        {
            _connectionFactory = connectionFactory;
            _diagnosticsConfig = diagnosticsConfig;
        }

        public async Task<IEnumerable<BankTransaction>> GetTransactionsByCustomerIdAsync(int customerId, int limit = 1000, IDbConnection? connection = null)
        {
#if DEBUG
            using var activity = _diagnosticsConfig.Source.StartActivity("TransactionRepository.GetTransactionsByCustomerIdAsync()");
#endif
            await using var command = getTransactionsCommand.Clone();

            command.Parameters[0].Value = customerId;
            command.Parameters[1].Value = limit;

            if (connection == null)
            {
                await using var newConnection = await _connectionFactory.GetConnectionAsync();
                command.Connection = newConnection;
            }
            else
                command.Connection = (NpgsqlConnection)connection;

            await using var reader = await command.ExecuteReaderAsync();

            var transactions = new List<BankTransaction>();
            while (await reader.ReadAsync())
            {
                var bankTransaction = new BankTransaction
                {
                    Id = reader.GetInt32("transaction_id"),
                    Amount = reader.GetInt32("transaction_amount"),
                    Type = reader.GetString("transaction_type"),
                    Description = reader.GetString("transaction_description"),
                    CreatedAt = reader.GetDateTime("created_at")
                };

                transactions.Add(bankTransaction);
            }

            return transactions;
        }

        public async Task<bool> InsertAsync(BankTransaction transaction, IDbConnection? connection = null)
        {
#if DEBUG
            using var activity = _diagnosticsConfig.Source.StartActivity("TransactionRepository.InsertAsync()");
#endif
            await using var command = insertCommand.Clone();

            command.Parameters[0].Value = transaction.CustomerId;
            command.Parameters[1].Value = transaction.Amount;
            command.Parameters[2].Value = transaction.Type;
            command.Parameters[3].Value = transaction.Description;

            if (connection == null)
            {
                await using var newConnection = await _connectionFactory.GetConnectionAsync();
                command.Connection = newConnection;
            }
            else
                command.Connection = (NpgsqlConnection)connection;

            var rows = await command.ExecuteNonQueryAsync();

            return rows > 0;
        }
    }
}
