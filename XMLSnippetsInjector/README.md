# XMLSnippetsInjector

A .NET 10 console application that automatically injects code examples from C# source files into XML documentation files by matching code snippets with XML documentation members.

## Purpose

XMLSnippetsInjector automates the process of enhancing XML documentation files with code examples extracted from C# source files. It scans C# files for specially marked code regions, matches them to corresponding XML documentation members, and injects them as `<example>` elements. This helps maintain synchronized code examples in documentation without manual copy-paste.

## How It Works

1. **Snippet Extraction**: Scans C# source files for code snippets marked with `//cref:` comments followed by `#region` blocks
2. **XML Loading**: Loads XML documentation files and parses member elements
3. **Matching**: Matches snippet references (crefs) to XML member names using various strategies (exact match, normalized match)
4. **Injection**: Injects matched snippets as `<example>` elements with `<code>` children into the corresponding XML members
5. **Reporting**: Generates detailed reports about injections, unused snippets, and processing statistics

## Usage

```
XMLSnippetsInjector [options] <xmlInputDir> <csInputDir> <outputDir>
```

### Arguments

- `<xmlInputDir>` - Directory containing XML documentation files to process
- `<csInputDir>` - Directory containing C# source files with code snippets
- `<outputDir>` - Directory where processed XML files and reports will be written

### Options

- `-c, --clear` - Clear the output directory before processing
- `-r, --remove-examples` - Remove existing `<example>` nodes that reference shared path snippets (configurable in appsettings.json)
- `-a, --remove-all-examples` - Remove all existing `<example>` nodes regardless of source
- `-p, --process-all-examples` - Process and transform all existing `<example>` nodes by wrapping content in `<code>` tags with descriptions from parent `<summary>` elements

### Examples

Basic usage:
```powershell
XMLSnippetsInjector C:\Docs\XML C:\Source\CS C:\Output
```

Clear output directory before processing:
```powershell
XMLSnippetsInjector --clear C:\Docs\XML C:\Source\CS C:\Output
```

Remove shared examples and inject new ones:
```powershell
XMLSnippetsInjector -r C:\Docs\XML C:\Source\CS C:\Output
```

Process all existing examples and add new ones:
```powershell
XMLSnippetsInjector -p C:\Docs\XML C:\Source\CS C:\Output
```

## Snippet Format

Code snippets in C# files must follow this format:

```csharp
//cref: Namespace.ClassName.MethodName
#region Region Name
// Your code example here
var example = new Example();
example.DoSomething();
#endregion
```

**Key Points:**
- The `//cref:` comment must appear before the `#region`
- The text after `//cref:` should match the XML member name (without the prefix like `M:`, `T:`, etc.)
- Multiple `//cref:` comments can reference the same region
- The region name becomes the `description` attribute in the generated `<code>` element
- Blank lines or additional comments are allowed between `//cref:` and `#region`

## Output

### Modified XML Files

Processed XML files are written to the output directory maintaining the original directory structure. Files are only written if changes were made (injections or removals).

Example of injected content:
```xml
<member name="M:Namespace.ClassName.MethodName">
	<summary>Method description</summary>
	<example>
		<code description="Region Name" cref="Namespace.ClassName.MethodName">
// Your code example here
var example = new Example();
example.DoSomething();
		</code>
	</example>
</member>
```

### Reports

Reports are generated in `<outputDir>/reports/`:

1. **summary.txt** - Overview of processing results:
	 - Total XML files processed
	 - Files with/without injections
	 - Total snippets found, used, and unused
	 - Injections per XML file

2. **injection-report.txt** - Detailed list of all successful injections showing:
	 - Snippet ID and cref
	 - Source file location
	 - Target XML file and member

3. **unused-snippets.txt** - Snippets that couldn't be matched to any XML member:
	 - Snippet details and code
	 - Sample of attempted matches

4. **xml-members-without-snippets.txt** - XML members that received no injections

5. **xml-files-with-no-injections.txt** - XML files that had no changes

6. **cref-usage.txt** - Statistics by cref showing:
	 - Which crefs had snippets used/unused
	 - Source files for each cref
	 - Overall usage statistics

7. **xmlsnippetsinjector-{timestamp}.log** - Detailed execution log with timestamps

## Configuration

The application can be configured via `appsettings.json`:

```json
{
	"SharedPath": "ArcGIS\\SharedArcGIS",
	"InternalNamespace": ".Internal."
}
```

**Settings:**
- `SharedPath` - Path pattern used by the `--remove-examples` flag to identify snippets to remove
- `InternalNamespace` - Namespace marker to skip internal members during processing (members containing this string are ignored)

## Matching Strategy

The tool uses multiple matching strategies in order of preference:

1. **Exact Match**: Direct match between snippet cref and XML member name (with or without prefix like `M:`)
2. **Normalized Match**: Removes parameter lists and normalizes whitespace before comparing

Members in namespaces containing the configured `InternalNamespace` marker are automatically skipped.

## Use Cases

- **Documentation Generation**: Automatically populate API documentation with working code examples
- **Example Synchronization**: Keep code examples in sync with actual source code
- **Bulk Documentation**: Process large codebases to add examples to XML documentation
- **Example Cleanup**: Remove outdated or unwanted examples before re-injecting current ones
- **Example Transformation**: Convert simple example text to structured code elements with descriptions

## Requirements

- .NET 8.0 or higher
- Write access to output directory
- Valid XML documentation files
- C# source files with properly formatted snippet markers

## Exit Codes

- `0` - Success
- `1` - Error (invalid arguments, missing directories, or processing failure)

## Logging

All operations are logged with timestamps. The log file is initially created in the current directory, then moved to `<outputDir>/reports/` once the output directory is established. This ensures all processing details are captured even if early initialization fails.

## Notes

- The tool preserves existing XML files that don't need changes by copying them to the output directory
- Snippets are uniquely identified by their source file name and index within that file
- Duplicate injections are prevented - the same snippet won't be injected twice into the same member
- XML documents are loaded entirely into memory for processing
- The tool is case-insensitive for path comparisons but preserves original casing in output
