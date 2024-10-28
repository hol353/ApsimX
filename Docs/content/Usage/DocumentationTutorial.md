---
title: "Writing Documentation for Models"
draft: false
---
<p style="font-size: 10px">Created 21/10/2024 - Last updated 21/10/2024</p>

## APSIM Documentation System

As of October 2024, the APSIM Documentation system has been rebuilt into a centralised library called APSIM.Documentation, that removes all the C# documentation code of models from the Document() function that used to exist in each model's class.

The code for generating documentation is wrapped up within the `AutoDocumentation` class, which is now the main access point for any generation of docs. The entrypoint for the class is the static AutoDocumentation.Document(`IModel`) function that takes an APSIM model and produces all the required documentation for it. To run documentation over an entire apsimx file, the root `Simulations` model is passed to this function.

### Documenting a C# Class

By default all C# classes that are a type of `IModel` are documented using a `DocGeneric` class that outputs the name of the model, any summary or remarks in the C# class, and the contents of any `Memo` children that the class has. For classes that have specific documentation requirements that need more than that, a `Doc[ModelName]` class is created and linked to be used instead. For example, to document `Leaf`, a class called `DocLeaf` is used instead of the `DocGeneric` class. All the custom documentation classes are a subclass of `DocGeneric` and most will first document title, summary, remarks and memos before adding additional information in order to maintain consistency across the documentation.

Model Classes are not linked to Documentation Class by name, instead a dictionary called documentMap within `AutoDocumentation` is used which tells the program which models use which documentation classes. Models not listed in the dictionary will use the `DocGeneric` class. Model documentation classes are all stored within APSIM.Documentation/Models/Types, and always in the format `Doc[ModelName]`.

AutoDocumention works by generating a tags structure, where elements are converted into approiate tags, then placed within sections for structuring the document. Only sections can hold other tags, as they are used to define headings and sub headings in the documents.

#### Tags:
- Section: Displays a title and holds child tags
- Paragraph: Displays text
- Table: Holds a DataTable which is shown as a table
- Image: Displays an Image
- Graph: Displays a Graph
- GraphPage: Displays multiple graphs on a page

To develop a new class documentation, the easist way is to copy an existing an documentation class and rename it, link it to the model class and then edit the Document() function to display the information required. Most documentation will begin with a called to make a summary and remarks section, which can be filled out using summary and remarks tags at the top of the C# class file.

		Section section = GetSummaryAndRemarksSection(model);

After entering all the content that you require in the documentation, the function then needs to return the tag structure. Sections will be numbered based on the structure of the tags, with sibling sections incrementing the current number, and child sections adding a subpoint to the heading number.

Formatting from memos is also read into the section structure, so if there are unintended section breaks and numbering, check if the content is coming from memos that have been written.

A simple documentation class looks like this example:

**DocMyModelClass.cs** ( Stored under APSIM.Documentation/Models/Types/ )

	using System.Collections.Generic;
	using APSIM.Shared.Documentation;
	using Models.Core;

	namespace APSIM.Documentation.Models.Types
	{

		/// <summary>
		/// Documentation class for MyModelClass
		/// </summary>
		public class DocMyModelClass : DocGeneric
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="DocMyModelClass" /> class.
			/// </summary>
			public DocMyModelClass(IModel model) : base(model) { }

			/// <summary>
			/// Document the model.
			/// </summary>
			public override List<ITag> Document(int none = 0)
			{
				Section section = GetSummaryAndRemarksSection(model);
				
				Section customSection = new Section("My Custom Section");
				customSection.Add(new Paragraph("A Paragraph of Text"));
				customSection.Add(new Paragraph("A 2nd Paragraph of Text"));

				return new List<ITag>() {section, customSection};
			}
		}
	}

Then add a link to the documentation class in AutoDocumentation.cs:

**AutoDocumentation.cs** ( Approximately Lines 49-57 )

	/// <summary>Returns a dictionary to match model classes to document class.</summary>
	private static Dictionary<Type, Type> DefineFunctions()
	{
		Dictionary<Type, Type> documentMap = new()
		{
			{typeof(MyModelClass), typeof(DocMyModelClass)},
			{typeof(Plant), typeof(DocPlant)},
			{typeof(PastureSpecies), typeof(DocPlant)},
			{typeof(Sugarcane), typeof(DocPlant)},

### Documenting a Plant Model

Plant models are kept in .json resource files under Models/Resources/ and provide the structure of models used within APSIM. The custom DocPlant documentation class will investigate and pull out information about a plant when documentation is generated for a Plant and the json resource has been loaded, such as when selecting to create documentation from the context menu in the GUI, or when creating a validation documentation from a apsimx file in the Tests/Validation folder.

As long as your plant model is of type `Plant`, it will use the DocPlant documentation class with the following details. If you instead have a model like `Sugarcane`, it will have to be manually linked to DocPlant in the AutoDocumentation dictionary and tested that it has all the correct components to display.

**Plant Documentation Sections**
1. Memos: All content from memos at the top of the resource file.
2. Plant Model Components: A table list of all the components that make up the plant and the types they have.
3. Composite Biomass: A table list of the all the composite biomass models that the plant has.
4. Cultivars: A table list of all the available cultivars and any alternative names (alias) that they have.
5. Child Components: Generated Documentation for each of the Child Components (excluding composite biomass models).
6. Biblography: List of references used in the document.

When making a new plant model, it is important to generate the documentation for your plant and make sure the information that it produces is correct and that sections are not missing. You should not need to make changes to the DocPlant class for a new plant model, and it is up to the author to include memos in their model to decribe how it was developed and can be used.

### Documenting a Validation Set

In the current build system, model documentation is generated for this website when testing a new build. To make this, a list of preselected apsimx files under Tests/Validation/ are used to generate the required documents. This is achieved by documenting the root Simulations node for that file, which has a special case in DocSimulations for apsimx files within that directory.

For those files, any memos attached to the Simulations node are shown, then the documentation of the plant model that matches the file's name, followed by information about the experiments and simulation that it contains. It will also render out any graphs under folders that have been set to appear in documentation, showing the results of those validation comparisions.

Currently there is no way to use the commandline to generate documentation of a single apsimx file, this functionality is wrapped up in the APSIM.Documentation program.

### Documenting a Tutorial

In the current build system, tutorials are generated for this website when testing a new build. To make this, a list of preselected apsimx files under Examples/Tutorials/ are used to generate the required documents. This is achieved by documenting the root Simulations node for that file, which has a special case in DocSimulations for tutorial files.

Tutorial content is written in memos within an apsimx file, so when generating a document, DocSimulations has a special case for tutorials so that it only pulls out the Memos, Simulations, folders and graphs, with the main aim to only include sections that help the user follow the tutorial instructions.

To create a new tutorial, first build an apsim file with a memo attached to Simulations, and save in within the Examples/Tutorial folder. Fill out the memo with all the details that you need for the tutorial, including text, pictures, headings etc, written in markdown. Lastly to have the tutorial get generated for display on the website, edit it in as a row into APSIM.Documentation/Progam.cs, using one of the existing tutorials as a reference.

<i><span style="color:red;">Note:</span> If you found any incorrect/outdated information in this tutorial. Please let us know on GitHub by <a href="https://www.github.com/APSIMInitiative/ApsimX/issues/new/choose/">submitting an issue.</a></i>