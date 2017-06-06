﻿using System;
using System.Collections.Generic;
using System.Text;
using Marten.Storage;
using Marten.Util;
using Npgsql;

namespace Marten.Schema
{
    public sealed class DatabaseGenerator : IDatabaseCreationExpressions, IDatabaseGenerator
    {
        private string _maintenanceDbConnectionString;
        private readonly Dictionary<string, TenantDatabaseCreationExpressions> _configurationPerTenant = new Dictionary<string, TenantDatabaseCreationExpressions>();

        public IDatabaseCreationExpressions MaintenanceDatabase(string maintenanceDbConnectionString)
        {
            _maintenanceDbConnectionString = maintenanceDbConnectionString ?? throw new ArgumentNullException(nameof(maintenanceDbConnectionString));

            return this;
        }        

        public ITenantDatabaseCreationExpressions ForTenant(string tenantId = Tenancy.DefaultTenantId)
        {
            var configurator = new TenantDatabaseCreationExpressions();
            _configurationPerTenant.Add(tenantId, configurator);
            return configurator;
        }
    
        private sealed class TenantDatabaseCreationExpressions : ITenantDatabaseCreationExpressions
        {
            private readonly StringBuilder _createOptions = new StringBuilder();
            public bool DropExistingDatabase { get; private set; }
            public bool CheckAgainstCatalog { get; private set; }
            public Action<NpgsqlConnection> OnDbCreated { get; private set; }

            public ITenantDatabaseCreationExpressions DropExisting()
            {
                DropExistingDatabase = true;
                return this;
            }

            public ITenantDatabaseCreationExpressions WithEncoding(string encoding)
            {
                _createOptions.Append($" ENCODING = '{encoding}'");
                return this;
            }

            public ITenantDatabaseCreationExpressions WithOwner(string owner)
            {
                _createOptions.Append($" OWNER = '{owner}'");
                return this;
            }

            public ITenantDatabaseCreationExpressions ConnectionLimit(int limit)
            {
                _createOptions.Append($" CONNECTION LIMIT = {limit}");
                return this;
            }

            public ITenantDatabaseCreationExpressions LcCollate(string lcCollate)
            {
                _createOptions.Append($" LC_COLLATE = '{lcCollate}'");
                return this;
            }

            public ITenantDatabaseCreationExpressions LcType(string lcType)
            {
                _createOptions.Append($" LC_CTYPE = '{lcType}'");
                return this;
            }

            public ITenantDatabaseCreationExpressions TableSpace(string tableSpace)
            {
                _createOptions.Append($" TABLESPACE  = {tableSpace}");
                return this;
            }

            public ITenantDatabaseCreationExpressions CheckAgainstPgDatabase()
            {
                CheckAgainstCatalog = true;
                return this;
            }

            public ITenantDatabaseCreationExpressions OnDatabaseCreated(Action<NpgsqlConnection> onDbCreated)
            {
                OnDbCreated = onDbCreated;
                return this;
            }            

            public override string ToString()
            {
                return _createOptions.ToString();
            }
        }

        public void CreateDatabases(ITenancy tenancy, Action<IDatabaseCreationExpressions> configure)
        {
            configure(this);

            foreach (var tenantConfig in _configurationPerTenant)
            {
                var tenant = tenancy[tenantConfig.Key];
                var config = tenantConfig.Value;

                CreateDb(tenant, config);
            }
            
        }

        private void CreateDb(ITenant tenant, TenantDatabaseCreationExpressions config)
        {
            string catalog;

            using (var t = tenant.CreateConnection())
            {
                catalog = t.Database;

                var noExistingDb = config.CheckAgainstCatalog
                    ? new Func<bool>(() => IsNotInPgDatabase(catalog))
                    : (() => CannotConnectDueToInvalidCatalog(t));

                if (noExistingDb())
                {
                    CreateDb(catalog, config, false);
                    return;
                }
            }

            if (config.DropExistingDatabase)
            {
                CreateDb(catalog, config, true);
            }
        }

        private bool IsNotInPgDatabase(string catalog)
        {
            using (var connection = new NpgsqlConnection(_maintenanceDbConnectionString))
            using (var cmd = connection.CreateCommand("SELECT datname FROM pg_database where datname = @catalog"))
            {
                cmd.AddNamedParameter("catalog", catalog);

                connection.Open();

                try
                {
                    var m = cmd.ExecuteScalar();
                    return m == null;
                }
                finally
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        private bool CannotConnectDueToInvalidCatalog(NpgsqlConnection t)
        {
            try
            {
                t.Open();
                t.Close();
            }
            // INVALID CATALOG NAME (https://www.postgresql.org/docs/current/static/errcodes-appendix.html)
            catch (PostgresException e) when (e.SqlState == "3D000")
            {                
                return true;
            }
            return false;
        }

        private void CreateDb(string catalog, TenantDatabaseCreationExpressions config, bool dropExisting)
        {
            var cmdText = string.Empty;

            if (dropExisting)
            {
                cmdText = $"DROP DATABASE IF EXISTS {catalog};";
            }

            using (var connection = new NpgsqlConnection(_maintenanceDbConnectionString))
            using (var cmd = connection.CreateCommand(cmdText))
            {
                cmd.CommandText += $"CREATE DATABASE {catalog} WITH" + config;
                connection.Open();
                try
                {
                    cmd.ExecuteNonQuery();
                    config.OnDbCreated?.Invoke(connection);
                }                
                finally
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }
    }
}