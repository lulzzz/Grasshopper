﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Grasshopper.Platform
{
    public abstract class PlatformResourceManager<T> : IPlatformResourceManager<T>
        where T : IPlatformResource
    {
        private readonly Dictionary<string, T> _resources = new Dictionary<string, T>();

        public IEnumerator<T> GetEnumerator()
        {
            return _resources.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[string id]
        {
            get
            {
                T resource;
                if(!_resources.TryGetValue(id, out resource))
                    throw new ArgumentOutOfRangeException("id", "The resource '" + id + "' was not found");
                return resource;
            }
        }

        protected T Add(T resource, bool initialize = true)
        {
            if(_resources.ContainsKey(resource.Id))
                throw new ArgumentException("A resource with the id '" + resource.Id + "' has already been added");

            resource.Initialized += OnResourceInitialized;
            resource.Uninitialized += OnResourceUninitialized;
            resource.Disposed += OnResourceDisposed;

            _resources.Add(resource.Id, resource);
    
            var handler = ResourceAdded;
            if(handler != null)
                handler(resource);

            if(initialize)
                resource.Initialize();

            return resource;
        }

        public T Remove(string id)
        {
            if(!_resources.ContainsKey(id))
                return default(T);

            var resource = _resources[id];
            _resources.Remove(id);

            resource.Initialized -= OnResourceInitialized;
            resource.Uninitialized -= OnResourceUninitialized;
            resource.Disposed -= OnResourceDisposed;

            var handler = ResourceRemoved;
            if(handler != null)
                handler(resource);

            return resource;
        }

        public void RemoveAndDispose(string id)
        {
            var resource = Remove(id);
            if(resource != null)
                resource.Dispose();
        }

        public void Initialize(string id)
        {
            this[id].Initialize();
        }

        public void Uninitialize(string id)
        {
            this[id].Uninitialize();
        }

        public bool Exists(string id)
        {
            return _resources.ContainsKey(id);
        }

        public bool IsInitialized(string id)
        {
            return this[id].IsInitialized;
        }

        public event PlatformResourceEventHandler<T> ResourceAdded;
        public event PlatformResourceEventHandler<T> ResourceRemoved;
        public event PlatformResourceEventHandler<T> ResourceInitialized;
        public event PlatformResourceEventHandler<T> ResourceUninitialized;
        public event PlatformResourceEventHandler<T> ResourceDisposed;
        public event Action Disposing;
        public event Action Disposed;

        private void OnResourceInitialized(IPlatformResource resource)
        {
            var handler = ResourceInitialized;
            if(handler != null)
                handler((T)resource);
        }

        private void OnResourceUninitialized(IPlatformResource resource)
        {
            var handler = ResourceUninitialized;
            if(handler != null)
                handler((T)resource);
        }

        private void OnResourceDisposed(IPlatformResource resource)
        {
            Remove(resource.Id);
            var handler = ResourceDisposed;
            if(handler != null)
                handler((T)resource);
        }

        public virtual void Dispose()
        {
            var handler = Disposing;
            if(handler != null)
                handler();
            
            foreach(var resource in _resources.Values.ToArray())
                resource.Dispose();
            _resources.Clear();
            
            handler = Disposed;
            if(handler != null)
                handler();
        }
    }
}