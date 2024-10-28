﻿using APSIM.Shared.Extensions;
using APSIM.Shared.Utilities;
using APSIM.Documentation.Models;
using Models.Core;
using Models.Functions;
using System;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Views;
using Models.PMF.Phen;

namespace UserInterface.Presenters
{
    public class DocumentationPresenter : IPresenter
    {
        private IMarkdownView view;
        private ExplorerPresenter presenter;
        private IModel model;


        public DocumentationPresenter()
        {
        }

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = view as IMarkdownView;
            this.model = model as IModel;
            this.presenter = explorerPresenter;

            PopulateView();
        }

        private async void PopulateView()
        {
            view.Text = await Task.Run(() => DocumentModel(model).Replace("<", @"\<"));
        }

        private string DocumentModel(IModel model)
        {
            StringBuilder markdown = new StringBuilder();

            string summary = AutoDocumentation.GetSummary(model.GetType());
            markdown.AppendLine($"# {model.Name} Description");
            markdown.AppendLine();
            markdown.AppendLine(summary);
            markdown.AppendLine();

            string remarks = AutoDocumentation.GetRemarks(model.GetType());
            if (!string.IsNullOrEmpty(remarks))
            {
                markdown.AppendLine($"# Remarks");
                markdown.AppendLine();
                markdown.AppendLine(remarks);
                markdown.AppendLine();
            }

            //This has been added so Phenology can show its phases inside of the GUI
            if (model.GetType() == typeof(Phenology))
            {
                DataTable dataTable = (model as Phenology).GetPhaseTable();
                markdown.AppendLine(DataTableUtilities.ToMarkdown(dataTable, true));
                markdown.AppendLine();
            }

            string typeName = model.GetType().Name;
            DataTable functionTable = GetDependencies(model, m => typeof(IFunction).IsAssignableFrom(GetMemberType(m)));
            DataTable depsTable = GetDependencies(model, m => !typeof(IFunction).IsAssignableFrom(GetMemberType(m)));
            DataTable publicMethods = GetPublicMethods(model);
            DataTable events = GetEvents(model);
            DataTable outputs = GetOutputs(model);

            if (functionTable.Rows.Count > 0
             || depsTable.Rows.Count > 0
             || publicMethods.Rows.Count > 0
             || events.Rows.Count > 0
             || outputs.Rows.Count > 0)
            {
                markdown.AppendLine($"# {model.Name} Configuration");
                markdown.AppendLine();

                if (functionTable.Rows.Count > 0 || depsTable.Rows.Count > 0)
                {
                    markdown.AppendLine("## Inputs");
                    markdown.AppendLine();

                    if (functionTable.Rows.Count > 0)
                    {
                        markdown.AppendLine("### Variable Dependencies");
                        markdown.AppendLine();
                        markdown.AppendLine(DataTableUtilities.ToMarkdown(functionTable, true));
                        markdown.AppendLine();
                    }

                    if (depsTable.Rows.Count > 0)
                    {
                        markdown.AppendLine("### Fixed Dependencies");
                        markdown.AppendLine();
                        markdown.AppendLine(DataTableUtilities.ToMarkdown(depsTable, true));
                        markdown.AppendLine();
                    }
                }

                if (publicMethods.Rows.Count > 0)
                {
                    markdown.AppendLine("## Public Methods");
                    markdown.AppendLine();
                    markdown.AppendLine(DataTableUtilities.ToMarkdown(publicMethods, true));
                    markdown.AppendLine();
                }

                if (events.Rows.Count > 0)
                {
                    markdown.AppendLine("## Public Events");
                    markdown.AppendLine();
                    markdown.AppendLine(DataTableUtilities.ToMarkdown(events, true));
                    markdown.AppendLine();
                }

                if (outputs.Rows.Count > 0)
                {
                    markdown.AppendLine("## Outputs");
                    markdown.AppendLine();
                    markdown.AppendLine(DataTableUtilities.ToMarkdown(outputs, true));
                    markdown.AppendLine();
                }
            }

            return markdown.ToString();
        }

        private DataTable GetEvents(IModel model)
        {
            DataTable table = new DataTable("Public Events");
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Delegate Type", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            foreach (EventInfo evnt in model.GetType().GetEvents(flags))
            {
                if (!evnt.IsSpecialName)
                {
                    DataRow row = table.NewRow();

                    row[0] = evnt.Name;
                    row[1] = evnt.EventHandlerType.GetFriendlyName();
                    row[2] = AutoDocumentation.GetSummary(evnt);
                    row[3] = AutoDocumentation.GetRemarks(evnt);

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        private DataTable GetOutputs(IModel model)
        {
            DataTable table = new DataTable("Public Outputs");
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Units", typeof(string)));
            table.Columns.Add(new DataColumn("Type", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (PropertyInfo property in model.GetType().GetProperties(flags))
            {
                if (property.GetCustomAttribute<DescriptionAttribute>() == null)
                {
                    DataRow row = table.NewRow();

                    row[0] = property.Name;
                    row[1] = property.GetCustomAttribute<UnitsAttribute>()?.ToString();
                    row[2] = property.PropertyType.GetFriendlyName();
                    row[3] = AutoDocumentation.GetSummary(property);
                    row[4] = AutoDocumentation.GetRemarks(property);

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        private DataTable GetPublicMethods(IModel model)
        {
            DataTable table = new DataTable("Public Methods");
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Return type", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            foreach (MethodInfo method in model.GetType().GetMethods(flags))
            {
                if (!method.IsSpecialName)
                {
                    DataRow row = table.NewRow();

                    row[0] = method.Name;
                    row[1] = method.ReturnType.GetFriendlyName();
                    row[2] = AutoDocumentation.GetSummary(method);
                    row[3] = AutoDocumentation.GetRemarks(method);

                    table.Rows.Add(row);
                }
            }

            return table;
        }

        /// <summary>
        /// Get a table of all dependencies (Links) of a given model
        /// which match a given predicate.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="filter">Predicate on which to filter dependencies.</param>
        /// <returns></returns>
        private DataTable GetDependencies(IModel model, Predicate<MemberInfo> filter)
        {
            DataTable result = new DataTable("Functions");
            result.Columns.Add(new DataColumn("Name", typeof(string)));
            result.Columns.Add(new DataColumn("Type", typeof(string)));
            result.Columns.Add(new DataColumn("Link Type", typeof(string)));
            result.Columns.Add(new DataColumn("Link by Name", typeof(string)));
            result.Columns.Add(new DataColumn("Optional", typeof(string)));
            result.Columns.Add(new DataColumn("Path", typeof(string)));
            result.Columns.Add(new DataColumn("Description", typeof(string)));
            result.Columns.Add(new DataColumn("Remarks", typeof(string)));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MemberInfo member in model.GetType().GetMembers(flags))
            {
                LinkAttribute link = member.GetCustomAttribute<LinkAttribute>();
                if (link != null && filter(member))
                {
                    DataRow row = result.NewRow();
                    
                    row[0] = member.Name;
                    row[1] = GetMemberType(member).Name;
                    row[2] = link.Type.ToString();
                    row[3] = link.ByName.ToString();
                    row[4] = link.IsOptional.ToString();
                    row[5] = link.Path;
                    row[6] = AutoDocumentation.GetSummary(member);
                    row[7] = AutoDocumentation.GetRemarks(member);

                    result.Rows.Add(row);
                }
            }

            return result;
        }

        private Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo prop)
                return prop.PropertyType;

            if (member is FieldInfo field)
                return field.FieldType;

            if (member is MethodInfo)
                throw new NotImplementedException(); // return method.ReturnType;

            throw new Exception($"Unknown member type on member {member.Name} of type {member.DeclaringType.Name}: {member.GetType().Name}");
        }

        public void Detach()
        {
        }
    }
}
