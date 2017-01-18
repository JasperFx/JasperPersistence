﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Baseline;
using Marten.Linq;
using Marten.Schema;
using Marten.Schema.Identity;
using Marten.Schema.Identity.Sequences;

namespace Marten
{
    /// <summary>
    /// Used to customize or optimize the storage and retrieval of document types
    /// </summary>
    public class MartenRegistry
    {
        private readonly StoreOptions _options;
        private readonly IList<Action<StoreOptions>> _alterations = new List<Action<StoreOptions>>();

        public MartenRegistry()
        {
        }

        public MartenRegistry(StoreOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Configure a single document type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public DocumentMappingExpression<T> For<T>()
        {
            return new DocumentMappingExpression<T>(this);
        }

        private Action<StoreOptions> alter
        {
            set
            {
                if (_options != null)
                {
                    value(_options);
                }

                _alterations.Add(value);
            }
        }

        /// <summary>
        /// Include the declarations from another MartenRegistry type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Include<T>() where T : MartenRegistry, new()
        {
            alter = x =>
            {
                var registry = new T();
                registry._alterations.Each(a => alter = a);
            };
        }

        /// <summary>
        /// Include the declarations from another MartenRegistry object
        /// </summary>
        /// <param name="registry"></param>
        public void Include(MartenRegistry registry)
        {
            registry._alterations.Each(a => alter = a);
        }

        /// <summary>
        /// Overrides the strategy used to generate the ids.
        /// </summary>
        public void DefaultIdStrategy(Func<IDocumentMapping, StoreOptions, IIdGeneration> strategy)
        {
            alter = x => x.DefaultIdStrategy = strategy;
        }

        public class DocumentMappingExpression<T>
        {
            private readonly MartenRegistry _parent;

            public DocumentMappingExpression(MartenRegistry parent)
            {
                _parent = parent;

                _parent.alter = options => options.MappingFor(typeof (T));
            }

            /// <summary>
            /// Specify the property searching mechanism for this document type. The default is
            /// JSON_Locator_Only
            /// </summary>
            /// <param name="searching"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> PropertySearching(PropertySearching searching)
            {
                alter = m => m.PropertySearching = searching; 
                return this;
            }

            /// <summary>
            /// Override the Postgresql schema alias for this document type in order
            /// to disambiguate similarly named document types. The default is just
            /// the document type name to lower case.
            /// </summary>
            /// <param name="alias"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> DocumentAlias(string alias)
            {
                alter = m => m.Alias = alias;
                return this;
            }  

            /// <summary>
            /// Marks a property or field on this document type as a searchable field that is also duplicated in the 
            /// database document table
            /// </summary>
            /// <param name="expression"></param>
            /// <param name="pgType">Optional, overrides the Postgresql column type for the duplicated field</param>
            /// <param name="configure">Optional, allows you to customize the Postgresql database index configured for the duplicated field</param>
            /// <returns></returns>
            [Obsolete("Prefer Index() if you just want to optimize querying, or choose Duplicate() if you really want a duplicated field")]
            public DocumentMappingExpression<T> Searchable(Expression<Func<T, object>> expression, string pgType = null, Action<IndexDefinition> configure = null)
            {
                return Duplicate(expression, pgType, configure);
            }

            /// <summary>
            /// Marks a property or field on this document type as a searchable field that is also duplicated in the 
            /// database document table
            /// </summary>
            /// <param name="expression"></param>
            /// <param name="pgType">Optional, overrides the Postgresql column type for the duplicated field</param>
            /// <param name="configure">Optional, allows you to customize the Postgresql database index configured for the duplicated field</param>
            /// <returns></returns>
            public DocumentMappingExpression<T> Duplicate(Expression<Func<T, object>> expression, string pgType = null, Action<IndexDefinition> configure = null)
            {
                alter = mapping => mapping.Duplicate(expression, pgType, configure);
                return this;
            }

            /// <summary>
            /// Creates a computed index on this data member within the JSON data storage
            /// </summary>
            /// <param name="expression"></param>
            /// <param name="configure"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> Index(Expression<Func<T, object>> expression, Action<ComputedIndex> configure = null)
            {
                alter = m => m.Index(expression, configure);

                return this;
            }

            /// <summary>
            /// Creates an index on the predefined Last Modified column
            /// </summary>
            /// <param name="configure"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> IndexLastModified(Action<IndexDefinition> configure = null)
            {
                alter = m => m.AddLastModifiedIndex(configure);

                return this;
            }

            public DocumentMappingExpression<T> ForeignKey<TReference>(
                Expression<Func<T, object>> expression,
                Action<ForeignKeyDefinition> foreignKeyConfiguration = null,
                Action<IndexDefinition> indexConfiguration = null)
            {
                alter = m => m.ForeignKey<TReference>(expression, foreignKeyConfiguration, indexConfiguration);

                return this;
            }

            /// <summary>
            /// Overrides the Hilo sequence increment and "maximum low" number for document types that
            /// use numeric id's and the Hilo Id assignment
            /// </summary>
            /// <param name="settings"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> HiloSettings(HiloSettings settings)
            {
                alter = mapping => mapping.HiloSettings(settings);
                return this;
            }

            /// <summary>
            /// Overrides the database schema name used to store the documents.
            /// </summary>
            public DocumentMappingExpression<T> DatabaseSchemaName(string databaseSchemaName)
            {
                alter = mapping => mapping.DatabaseSchemaName = databaseSchemaName;
                return this;
            }

            /// <summary>
            /// Overrides the stragtegy used for id generation.
            /// </summary>
            public DocumentMappingExpression<T> IdStrategy(IIdGeneration idStrategy)
            {
                alter = mapping => mapping.IdStrategy = idStrategy;
                return this;
            }

            private Action<DocumentMapping<T>> alter
            {
                set
                {
                    Action<StoreOptions> alteration = o =>
                    {
                        value((DocumentMapping<T>) o.MappingFor(typeof (T)));
                    };


                    _parent.alter = alteration;
                   
                }
            }

            /// <summary>
            /// Adds a Postgresql Gin index to the JSONB data column for this document type. Leads to faster
            /// querying, but does add overhead to storage and database writes
            /// </summary>
            /// <param name="configureIndex"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> GinIndexJsonData(Action<IndexDefinition> configureIndex = null)
            {
                alter = mapping =>
                {
                    var index = mapping.AddGinIndexToData();

                    configureIndex?.Invoke(index);
                };

                return this;
            }

            /// <summary>
            /// Programmatically directs Marten to map this type to a hierarchy of types
            /// </summary>
            /// <param name="subclassType"></param>
            /// <param name="alias"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> AddSubClass(Type subclassType, string alias = null)
            {
                alter = mapping => mapping.AddSubClass(subclassType, alias);
                return this;
            }

            /// <summary>
            /// Programmatically directs Marten to map all the subclasses of <cref name="T"/> to a hierarchy of types
            /// </summary>
            /// <param name="allSubclassTypes">All the subclass types of <cref name="T"/> that you wish to map. 
            /// You can use either params of <see cref="Type"/> or <see cref="MappedType"/> or a mix, since Type can implicitly convert to MappedType (without an alias)</param>
            /// <returns></returns>
            public DocumentMappingExpression<T> AddSubClassHierarchy(params MappedType[] allSubclassTypes)
            {
                alter = m => m.AddSubClassHierarchy(allSubclassTypes);
                return this;
            }

            /// <summary>
            /// Programmatically directs Marten to map all the subclasses of <cref name="T"/> to a hierarchy of types. <c>Unadvised in projects with many types.</c>
            /// </summary>
            /// <returns></returns>
            public DocumentMappingExpression<T> AddSubClassHierarchy()
            {
                alter = m => m.AddSubClassHierarchy();

                return this;
            }


            public DocumentMappingExpression<T> AddSubClass<TSubclass>(string alias = null) where TSubclass : T
            {
                return AddSubClass(typeof(TSubclass), alias);
            }

            /// <summary>
            /// Directs Marten to use the optimistic versioning checks upon updates
            /// to this document type
            /// </summary>
            /// <returns></returns>
            public DocumentMappingExpression<T> UseOptimisticConcurrency(bool enabled)
            {
                alter = m => m.UseOptimisticConcurrency = enabled;
                return this;
            }

            /// <summary>
            /// Directs Marten to apply "soft deletes" to this document type
            /// </summary>
            /// <returns></returns>
            public DocumentMappingExpression<T> SoftDeleted()
            {
                alter = m => m.DeleteStyle = DeleteStyle.SoftDelete;
                return this;
            }

            /// <summary>
            /// Direct this document type's DDL to be created with the named template
            /// </summary>
            /// <param name="templateName"></param>
            /// <returns></returns>
            public DocumentMappingExpression<T> DdlTemplate(string templateName)
            {
                alter = m => m.DdlTemplate = templateName;
                return this;
            }
        }
    }

    public class MappedType
    {
        public MappedType(Type type, string alias = null)
        {
            Type = type;
            Alias = alias;
        }
        public Type Type { get; set; }
        public string Alias { get; set; }

        public static implicit operator MappedType (Type type)
        {
            return new MappedType(type);
        }
    }
}
