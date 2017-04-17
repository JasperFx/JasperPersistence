﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baseline;
using Marten.Storage;

namespace Marten.Transforms
{
    public interface ITransforms
    {
        void LoadFile(string file, string name = null);
        void LoadDirectory(string directory);

        void LoadJavascript(string name, string script);

        void Load(TransformFunction function);

        TransformFunction For(string name);

        IEnumerable<TransformFunction> AllFunctions();
    }

    public class Transforms : ITransforms, IFeatureSchema
    {
        private readonly StoreOptions _options;

        private readonly IDictionary<string, TransformFunction> _functions 
            = new Dictionary<string, TransformFunction>();

        public Transforms(StoreOptions options)
        {
            _options = options;
        }

        public void LoadFile(string file, string name = null)
        {
            if (!Path.IsPathRooted(file))
            {
                file = AppContext.BaseDirectory.AppendPath(file);
            }

            var function = TransformFunction.ForFile(_options, file, name);
            _functions.Add(function.Name, function);
        }

        public void LoadDirectory(string directory)
        {
            if (!Path.IsPathRooted(directory))
            {
                directory = AppContext.BaseDirectory.AppendPath(directory);
            }

            new FileSystem().FindFiles(directory, FileSet.Deep("*.js")).Each(file =>
            {
                LoadFile(file);
            });
        }

        public void LoadJavascript(string name, string script)
        {
            var func = new TransformFunction(_options, name, script);
            _functions.Add(func.Name, func);
        }

        public void Load(TransformFunction function)
        {
            _functions.Add(function.Name, function);
        }

        public TransformFunction For(string name)
        {
            if (!_functions.ContainsKey(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Unknown Transform Name");
            }

            return _functions[name];
        }

        public IEnumerable<TransformFunction> AllFunctions()
        {
            return _functions.Values;
        }

        public IEnumerable<Type> DependentTypes()
        {
            yield break;
        }

        // TODO -- turn this off if PLV8 is disabled
        public bool IsActive { get; } = true;

        public ISchemaObject[] Objects => _functions.Values.OfType<ISchemaObject>().ToArray();
        public Type StorageType { get; } = typeof(Transforms);
    }
}