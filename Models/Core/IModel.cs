using System;
using System.Collections.Generic;

namespace Models.Core
{

    /// <summary>
    /// The IModel interface specifies the properties and methods that all
    /// models must have. 
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        string Name { get; set; }

        /// <summary>The name of the resource.</summary>
        string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the parent model. Can be null if model has no parent.
        /// </summary>
        IModel Parent { get; set; }

        /// <summary>
        /// Gets of sets the child models. Can be empty array but never null.
        /// </summary>
        List<IModel> Children { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a model is hidden from the user.
        /// </summary>
        bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets whether the model is enabled
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether the model is readonly.
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Full path to the model.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// Find a sibling with a given name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        IModel FindSibling(string name);

        /// <summary>
        /// Find a child with a given name.
        /// </summary>
        /// <param name="name">Name of the child.</param>
        IModel FindChild(string name);

        /// <summary>
        /// Find a descendant with a given name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        IModel FindDescendant(string name);

        /// <summary>
        /// Find an ancestor with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        IModel FindAncestor(string name);

        /// <summary>
        /// Find a model in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        IModel FindInScope(string name);

        /// <summary>
        /// Find a sibling with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        T FindSibling<T>();

        /// <summary>
        /// Find a child with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the child.</typeparam>
        T FindChild<T>();

        /// <summary>
        /// Find a descendant with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        T FindDescendant<T>();

        /// <summary>
        /// Find an ancestor with a given type.
        /// </summary>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        T FindAncestor<T>();

        /// <summary>
        /// Find a model in scope with a given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of model to find.</typeparam>
        T FindInScope<T>();

        /// <summary>
        /// Find a sibling with a given type and name.
        /// </summary>
        /// <param name="name">Name of the sibling.</param>
        /// <typeparam name="T">Type of the sibling.</typeparam>
        T FindSibling<T>(string name);

        /// <summary>
        /// Find a child with a given type and name.
        /// </summary>
        /// <param name="name">Name of the child.</param>
        /// <typeparam name="T">Type of the child.</typeparam>
        T FindChild<T>(string name);

        /// <summary>
        /// Find a descendant model with a given type and name.
        /// </summary>
        /// <param name="name">Name of the descendant.</param>
        /// <typeparam name="T">Type of the descendant.</typeparam>
        T FindDescendant<T>(string name);

        /// <summary>
        /// Find an ancestor with a given type and name.
        /// </summary>
        /// <param name="name">Name of the ancestor.</param>
        /// <typeparam name="T">Type of the ancestor.</typeparam>
        T FindAncestor<T>(string name);

        /// <summary>
        /// Find a model in scope with a given type and name.
        /// </summary>
        /// <param name="name">Name of the model.</param>
        /// <typeparam name="T">Type of model to find.</typeparam>
        T FindInScope<T>(string name);

        /// <summary>
        /// Find all siblings with a given name.
        /// </summary>
        /// <param name="name">Name of the siblings.</param>
        IEnumerable<IModel> FindAllSiblings(string name);

        /// <summary>
        /// Find all children with a given name.
        /// </summary>
        /// <param name="name">Name of the children.</param>
        IEnumerable<IModel> FindAllChildren(string name);

        /// <summary>
        /// Find all descendants with a given name.
        /// </summary>
        /// <param name="name">Name of the descendants.</param>
        IEnumerable<IModel> FindAllDescendants(string name);

        /// <summary>
        /// Find all ancestors with a given name.
        /// </summary>
        /// <param name="name">Name of the ancestors.</param>
        IEnumerable<IModel> FindAllAncestors(string name);

        /// <summary>
        /// Find all models in scope with a given name.
        /// </summary>
        /// <param name="name">Name of the models.</param>
        IEnumerable<IModel> FindAllInScope(string name);

        /// <summary>
        /// Find all siblings of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> FindAllSiblings<T>();

        /// <summary>
        /// Find all children of the given type.
        /// </summary>
        /// <typeparam name="T">Type of children to return.</typeparam>
        IEnumerable<T> FindAllChildren<T>();

        /// <summary>
        /// Find all descendants of the given type.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        IEnumerable<T> FindAllDescendants<T>();

        /// <summary>
        /// Find all ancestors of the given type.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> FindAllAncestors<T>();

        /// <summary>
        /// Find all models of the given type in scope.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        IEnumerable<T> FindAllInScope<T>();

        /// <summary>
        /// Find all siblings with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of siblings to return.</typeparam>
        /// <param name="name">Name of the siblings.</param>
        IEnumerable<T> FindAllSiblings<T>(string name);

        /// <summary>
        /// Find all children with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of children to return.</typeparam>
        /// <param name="name">Name of the children.</param>
        IEnumerable<T> FindAllChildren<T>(string name);

        /// <summary>
        /// Find all descendants with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of descendants to return.</typeparam>
        /// <param name="name">Name of the descendants.</param>
        IEnumerable<T> FindAllDescendants<T>(string name);

        /// <summary>
        /// Find all ancestors with the given type and name.
        /// </summary>
        /// <typeparam name="T">Type of ancestors to return.</typeparam>
        /// <param name="name">Name of the ancestors.</param>
        IEnumerable<T> FindAllAncestors<T>(string name);

        /// <summary>
        /// Find all models with the given type and name in scope.
        /// </summary>
        /// <typeparam name="T">Type of models to find.</typeparam>
        /// <param name="name">Name of the models.</param>
        IEnumerable<T> FindAllInScope<T>(string name);

        /// <summary>
        /// Returns all ancestor models.
        /// </summary>
        IEnumerable<IModel> FindAllAncestors();

        /// <summary>
        /// Returns all descendant models.
        /// </summary>
        IEnumerable<IModel> FindAllDescendants();

        /// <summary>
        /// Returns all sibling models.
        /// </summary>
        IEnumerable<IModel> FindAllSiblings();

        /// <summary>
        /// Returns all children models.
        /// </summary>
        IEnumerable<IModel> FindAllChildren();

        /// <summary>
        /// Returns all models which are in scope.
        /// </summary>
        IEnumerable<IModel> FindAllInScope();

        /// <summary>
        /// Return true iff a model with the given type can be added to the model.
        /// </summary>
        /// <param name="type">The child type.</param>
        bool IsChildAllowable(Type type);

        /// <summary>
        /// Parent all descendant models.
        /// </summary>
        void ParentAllDescendants();

        /// <summary>
        /// Get the underlying variable object for the given path.
        /// Note that this can be a variable/property or a model.
        /// Returns null if not found.
        /// </summary>
        /// <param name="path">The path of the variable/model.</param>
        /// <param name="flags">LocatorFlags controlling the search</param>
        IVariable FindByPath(string path, LocatorFlags flags = LocatorFlags.None);

        /// <summary>
        /// Find and return multiple matches (e.g. a soil in multiple zones) for a given path.
        /// Note that this can be a variable/property or a model.
        /// Returns null if not found.
        /// </summary>
        /// <param name="path">The path of the variable/model.</param>
        IEnumerable<IVariable> FindAllByPath(string path);

        /// <summary>
        /// Called when the model has been newly created in memory whether from 
        /// cloning or deserialisation.
        /// </summary>
        void OnCreated();

        /// <summary>
        /// Called immediately before a simulation has its links resolved and is run.
        /// It provides an opportunity for a simulation to restructure itself 
        /// e.g. add / remove models.
        /// </summary>
        void OnPreLink();

    }
}
