﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Baseline;
using Marten.Linq;
using Marten.Schema;
using Marten.Schema.Sequences;

namespace Marten
{
    /// <summary>
    /// Used to customize or optimize the storage and retrieval of document types
    /// </summary>
    public class MartenRegistry
    {
        private readonly IList<Action<StoreOptions>> _alterations = new List<Action<StoreOptions>>();

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
            set { _alterations.Add(value); }
        }

        internal void Alter(StoreOptions schema)
        {
            _alterations.Each(x => x(schema));
        }

        /// <summary>
        /// Include the declarations from another MartenRegistry type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Include<T>() where T : MartenRegistry, new()
        {
            alter = x => new T().Alter(x);
        }

        /// <summary>
        /// Include the declarations from another MartenRegistry object
        /// </summary>
        /// <param name="registry"></param>
        public void Include(MartenRegistry registry)
        {
            alter = registry.Alter;
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
            public DocumentMappingExpression<T> Searchable(Expression<Func<T, object>> expression, string pgType = null, Action<IndexDefinition> configure = null)
            {
                var visitor = new FindMembers();
                visitor.Visit(expression);

                alter = mapping =>
                {
                    var duplicateField = mapping.DuplicateField(visitor.Members.ToArray(), pgType);
                    var indexDefinition = mapping.AddIndex(duplicateField.ColumnName);
                    configure?.Invoke(indexDefinition);
                };

                return this;
            }

            public DocumentMappingExpression<T> ForeignKey<TReference>(
                Expression<Func<T, object>> expression,
                Action<ForeignKeyDefinition> foreignKeyConfiguration = null,
                Action<IndexDefinition> indexConfiguration = null)
            {
                var visitor = new FindMembers();
                visitor.Visit(expression);

                alter = mapping =>
                {
                    var foreignKeyDefinition = mapping.AddForeignKey(visitor.Members.ToArray(), typeof(TReference));
                    foreignKeyConfiguration?.Invoke(foreignKeyDefinition);

                    var indexDefinition = mapping.AddIndex(foreignKeyDefinition.ColumnName);
                    indexConfiguration?.Invoke(indexDefinition);
                };

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

            private Action<DocumentMapping> alter
            {
                set
                {
                    Action<StoreOptions> alteration = o =>
                    {
                        value(o.MappingFor(typeof (T)));
                    };

                    _parent._alterations.Add(alteration);
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
            public DocumentMappingExpression<T> AddSubclass(Type subclassType, string alias = null)
            {
                alter = mapping => mapping.AddSubClass(subclassType, alias);
                return this;
            }


            public DocumentMappingExpression<T> AddSubclass<TSubclass>(string alias = null) where TSubclass : T
            {
                return AddSubclass(typeof(TSubclass), alias);
            }    
        }
    }
}