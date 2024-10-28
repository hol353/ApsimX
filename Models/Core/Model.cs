using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Models.Factorial;
using Newtonsoft.Json;

namespace Models.Core
{

    /// <summary>
    /// Base class for all models
    /// </summary>
    [Serializable]
    [ValidParent(typeof(Folder))]
    [ValidParent(typeof(Factor))]
    [ValidParent(typeof(CompositeFactor))]
    public abstract class Model : IModel
    {
        [NonSerialized]
        private IModel modelParent;

        private bool _enabled = true;
        private bool _isCreated = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Model" /> class.
        /// </summary>
        public Model()
        {
            this.Name = GetType().Name;
            this.IsHidden = false;
            this.Children = new List<IModel>();
            Enabled = true;
        }

        /// <summary>
        /// Gets or sets the name of the model
        /// </summary>
        public string Name { get; set; }

        /// <summary>The name of the resource.</summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets a list of child models.   
        /// </summary>
        public List<IModel> Children { get; set; }

        /// <summary>
        /// Gets or sets the parent of the model.
        /// </summary>
        [JsonIgnore]
        public virtual IModel Parent { get { return modelParent; } set { modelParent = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        [JsonIgnore]
        public bool IsHidden { get; set; }

        /// <summary>
        /// A cleanup routine, in which we clear our child list recursively
        /// </summary>
        public void ClearChildLists()
        {
            foreach (Model child in Children)
                child.ClearChildLists();
            Children.Clear();
        }

        /// <summary>
        /// Gets or sets whether the model is enabled
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                // enable / disable our children if serialisation has completed.
                if (_isCreated)
                    foreach (var child in Children)
                        child.Enabled = _enabled;
            }
        }

        /// <summary>
        /// Controls whether the model can be modified.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Full path to the model.
        /// </summary>
        public string FullPath
        {
            get
            {
                string fullPath = "." + Name;
                IModel parent = Parent;
                while (parent != null)
                {
                    fullPath = fullPath.Insert(0, "." + parent.Name);
                    parent = parent.Parent;
                }

                return fullPath;
            }
        }

        /// <summary>
        /// Find a sibling with a given name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        public IModel FindSibling(string name)
        {
            return FindAllSiblings(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a child with a given name.
        /// </summary>
        /// <param name="name">Name of the child.</param>
        public IModel FindChild(string name)
        {
            return FindAllChildren(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a descendant with a given name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        public IModel FindDescendant(string name)
        {
            return FindAllDescendants(name).FirstOrDefault();
        }

        /// <summary>
        /// Find an ancestor with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        public IModel FindAncestor(string name)
        {
            return FindAllAncestors(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a model in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        public IModel FindInScope(string name)
        {
            return FindAllInScope(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a sibling of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        public T FindSibling<T>()
        {
            return FindAllSiblings<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find a child with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the child.</typeparam>
        public T FindChild<T>()
        {
            return FindAllChildren<T>().FirstOrDefault();
        }

        /// <summary>
        /// Performs a depth-first search for a descendant of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        public T FindDescendant<T>()
        {
            return FindAllDescendants<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find an ancestor of a given type.
        /// </summary>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        public T FindAncestor<T>()
        {
            return FindAllAncestors<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find a model of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of model to find.</typeparam>
        public T FindInScope<T>()
        {
            return FindAllInScope<T>().FirstOrDefault();
        }

        /// <summary>
        /// Find a sibling with a given type and name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        public T FindSibling<T>(string name)
        {
            return FindAllSiblings<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a child with a given type and name.
        /// </summary>
        /// <param name="name">Name of the child.</param>
        /// <typeparam name="T">Type of the child.</typeparam>
        public T FindChild<T>(string name)
        {
            return FindAllChildren<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a descendant model with a given type and name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        public T FindDescendant<T>(string name)
        {
            return FindAllDescendants<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find an ancestor with a given type and name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        public T FindAncestor<T>(string name)
        {
            return FindAllAncestors<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find a model in scope with a given type and name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        /// <typeparam name="T">Type of model to find.</typeparam>
        public T FindInScope<T>(string name)
        {
            return FindAllInScope<T>(name).FirstOrDefault();
        }

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public IEnumerable<T> FindAllAncestors<T>()
        {
            return FindAllAncestors().OfType<T>();
        }

        /// <summary>
        /// Find all descendants of the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        public IEnumerable<T> FindAllDescendants<T>()
        {
            return FindAllDescendants().OfType<T>();
        }

        /// <summary>
        /// Find all siblings of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        public IEnumerable<T> FindAllSiblings<T>()
        {
            return FindAllSiblings().OfType<T>();
        }

        /// <summary>
        /// Find all children of the given type.
        /// </summary>
        /// <typeparam name="T">Type of children to return.</typeparam>
        public IEnumerable<T> FindAllChildren<T>()
        {
            return FindAllChildren().OfType<T>();
        }

        /// <summary>
        /// Find all models of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        public IEnumerable<T> FindAllInScope<T>()
        {
            return FindAllInScope().OfType<T>();
        }

        /// <summary>
        /// Find all siblings with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        /// <param name="name">Name of the siblings.</param>
        public IEnumerable<T> FindAllSiblings<T>(string name)
        {
            return FindAllSiblings(name).OfType<T>();
        }

        /// <summary>
        /// Find all children with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of children to return.</typeparam>
        /// <param name="name">Name of the children.</param>
        public IEnumerable<T> FindAllChildren<T>(string name)
        {
            return FindAllChildren(name).OfType<T>();
        }

        /// <summary>
        /// Find all descendants with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        /// <param name="name">Name of the descendants.</param>
        public IEnumerable<T> FindAllDescendants<T>(string name)
        {
            return FindAllDescendants(name).OfType<T>();
        }

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T">Type of ancestors to return.</typeparam>
        /// <param name="name">Name of the ancestors.</param>
        public IEnumerable<T> FindAllAncestors<T>(string name)
        {
            return FindAllAncestors(name).OfType<T>();
        }

        /// <summary>
        /// Find all models of a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        /// <param name="name">Name of the models.</param>
        public IEnumerable<T> FindAllInScope<T>(string name)
        {
            return FindAllInScope(name).OfType<T>();
        }

        /// <summary>
        /// Find all siblings with a given name.
        /// </summary>
        /// <param name="name">Name of the siblings.</param>
        public IEnumerable<IModel> FindAllSiblings(string name)
        {
            return FindAllSiblings().Where(s => s.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all children with a given name.
        /// </summary>
        /// <param name="name">Name of the children.</param>
        public IEnumerable<IModel> FindAllChildren(string name)
        {
            return FindAllChildren().Where(c => c.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all descendants with a given name.
        /// </summary>
        /// <param name="name">Name of the descendants.</param>
        public IEnumerable<IModel> FindAllDescendants(string name)
        {
            return FindAllDescendants().Where(d => d.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all ancestors with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestors.</param>
        public IEnumerable<IModel> FindAllAncestors(string name)
        {
            return FindAllAncestors().Where(a => a.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Find all model in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the models.</param>
        public IEnumerable<IModel> FindAllInScope(string name)
        {
            return FindAllInScope().Where(m => m.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Returns all ancestor models.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IModel> FindAllAncestors()
        {
            IModel parent = Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }

        /// <summary>
        /// Returns all descendant models.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IModel> FindAllDescendants()
        {
            foreach (IModel child in Children)
            {
                yield return child;

                foreach (IModel descendant in child.FindAllDescendants())
                    yield return descendant;
            }
        }

        /// <summary>
        /// Returns all sibling models.
        /// </summary>
        public IEnumerable<IModel> FindAllSiblings()
        {
            if (Parent == null || Parent.Children == null)
                yield break;

            foreach (IModel sibling in Parent.Children)
                if (sibling != this)
                    yield return sibling;
        }

        /// <summary>
        /// Returns all children models.
        /// </summary>
        public IEnumerable<IModel> FindAllChildren()
        {
            return Children;
        }

        /// <summary>
        /// Returns all models which are in scope.
        /// </summary>
        public IEnumerable<IModel> FindAllInScope()
        {
            Simulation sim = FindAncestor<Simulation>();
            ScopingRules scope = sim?.Scope ?? new ScopingRules();
            foreach (IModel result in scope.FindAll(this))
                yield return result;
        }

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        public virtual void OnCreated()
        {
            _isCreated = true;
            // Check for duplicate child models (child models with the same name).
            // First, group children according to their name.
            IEnumerable<IGrouping<string, IModel>> groups = Children.GroupBy(c => c.Name);
            foreach (IGrouping<string, IModel> group in groups)
            {
                int n = group.Count();
                if (n > 1)
                    throw new Exception($"Duplicate models found: {FullPath} has {n} children named {group.Key}");
            }
        }

        /// <summary>
        /// Called immediately before a simulation has its links resolved and is run.
        /// It provides an opportunity for a simulation to restructure itself 
        /// e.g. add / remove models.
        /// </summary>
        public virtual void OnPreLink() { }

        /// <summary>
        /// Return true iff a model with the given type can be added to the model.
        /// </summary>
        /// <param name="type">The child type.</param>
        public bool IsChildAllowable(Type type)
        {
            // Simulations objects cannot be added to any other models.
            if (typeof(Simulations).IsAssignableFrom(type))
                return false;

            // If it's not an IModel, it's not a valid child.
            if (!typeof(IModel).IsAssignableFrom(type))
                return false;

            List<Type> modelParentTypes = new();
            foreach(ValidParentAttribute validParent in ReflectionUtilities.GetAttributes(typeof(Model), typeof(ValidParentAttribute), true))
                modelParentTypes.Add(validParent.ParentType);

            bool hasValidParents = false;

            // Is allowable if one of the valid parents of this type (t) matches the parent type.
            foreach (ValidParentAttribute validParent in ReflectionUtilities.GetAttributes(type, typeof(ValidParentAttribute), true))
            {
                // Used to make objects that have no explicit ValidParent attributes allowed
                // to be placed anywhere.
                if (!modelParentTypes.Contains(validParent.ParentType))
                    hasValidParents = true;

                if (validParent != null)
                {
                    if (validParent.DropAnywhere)
                        return true;

                    if (validParent.ParentType != null && validParent.ParentType.IsAssignableFrom(GetType()))
                        return true;
                }
            }
            
            // If it doesn't have any valid parents, it should be able to be placed anywhere.
            if(hasValidParents)
                return false;
            else 
                return true;
        }

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// Note that this can be a variable/property or a model.
        /// Returns null if not found.
        /// </summary>
        /// <param name="path">The path of the variable/model.</param>
        /// <param name="flags">LocatorFlags controlling the search</param>
        /// <remarks>
        /// See <see cref="Locator"/> for more info about paths.
        /// </remarks>
        public IVariable FindByPath(string path, LocatorFlags flags = LocatorFlags.CaseSensitive | LocatorFlags.IncludeDisabled)
        {
            return Locator.GetObject(path, flags);
        }

        /// <summary>
        /// Find and return multiple matches (e.g. a soil in multiple zones) for a given path.
        /// Note that this can be a variable/property or a model.
        /// Returns null if not found.
        /// </summary>
        /// <param name="path">The path of the variable/model.</param>
        public IEnumerable<IVariable> FindAllByPath(string path)
        {
            IEnumerable<IModel> matches = null;

            // Remove a square bracketed model name and change our relativeTo model to 
            // the referenced model.
            if (path.StartsWith("["))
            {
                int posCloseBracket = path.IndexOf(']');
                if (posCloseBracket != -1)
                {
                    string modelName = path.Substring(1, posCloseBracket - 1);
                    path = path.Remove(0, posCloseBracket + 1).TrimStart('.');
                    matches = FindAllInScope(modelName);
                    if (!matches.Any())
                    {
                        // Didn't find a model with a name matching the square bracketed string so
                        // now try and look for a model with a type matching the square bracketed string.
                        Type[] modelTypes = ReflectionUtilities.GetTypeWithoutNameSpace(modelName, Assembly.GetExecutingAssembly());
                        if (modelTypes.Length == 1)
                            matches = FindAllInScope().Where(m => modelTypes[0].IsAssignableFrom(m.GetType()));
                    }
                }
            }
            else
                matches = new IModel[] { this };

            foreach (Model match in matches)
            {
                if (string.IsNullOrEmpty(path))
                    yield return new VariableObject(match);
                else
                {
                    var variable = match.Locator.GetObject(path, LocatorFlags.PropertiesOnly | LocatorFlags.CaseSensitive | LocatorFlags.IncludeDisabled);
                    if (variable != null)
                        yield return variable;
                }
            }
        }

        /// <summary>
        /// Parent all descendant models.
        /// </summary>
        public void ParentAllDescendants()
        {
            foreach (IModel child in Children)
            {
                child.Parent = this;
                child.ParentAllDescendants();
            }
        }

        /// <summary>A Locator object for finding models and variables.</summary>
        [NonSerialized]
        private Locator locator;

        /// <summary>Cache to speed up scope lookups.</summary>
        /// <value>The locater.</value>
        [JsonIgnore]
        public Locator Locator
        {
            get
            {
                if (locator == null)
                {
                    locator = new Locator(this);
                }
                return locator;
            }
        }
    }
}
