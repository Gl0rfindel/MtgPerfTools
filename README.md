# MtgPerfTools
Mod the Gungeon related performance tools for creating Enter the Gungeon mods

## Basics

There are three parts to the performance tools. The first MtgInstr is a command line instrumentation tool. 
Next is MtgProfilerTools. This library is loaded into the Enter the Gungeon process at runtime.
Finally the analyzer which processes the profile output into nice formats. The only one supported
currently is the speedscope format. Used by https://speedscope.app.

## Details

### MtgInstr

The instrumentation tool injects profiling code into the target assemblies. It can instrument a simple dll, a directory of dlls, or a zip.
There is special handling for Mod the Gungeon mods whereby only the primary mod dll is instrumented by default.

Running this tool requires a reference to both Assembly-CSharp.dll and MtgProfilerTools.dll make sure they are available. Use the -r option to 
set a reference path. If you build the tool in Debug mode an environment variable, MTGINSTR_RESOLVERPATH, is also available for this.

Use the --include-type and --exclude-type options to select the types to instrument in the target assemblies. Note that instrumenting frequently called
methods (e.g. Update) can bloat trace file size quickly.

### MtgProfilerTools

This library has handles writing profiling events out to disk while the game is running. You must set MTGPROFILER_DATADIR to a directory
where data will be saved to enable profiling. Also any errors will be written to separate log file in this folder.

To deploy this copy it next Assembly-CSharp. (i.e. in Enter the Gungon\EtG_Data\Managed)

As a safety precaution the output files are capped at 100MB by default. If the limit is hit a new file in create and the old one is immediately delete.
The file size can be changed by setting MTGPROFILER_MAXFILESIZE to another value (in MB).

### MtgProfileAnalyzer

This utility converts the raw output of the profiler into a consumable form. It has two commands:
  - analyze takes a list of input files and converts them.
  - watch takes a directory (or defaults to MTGPROFILER_DATADIR if no directory is specified) and watches for new files and converts them automatically.
 
To view the speedscope.json files generated, go to https://speedscope.app, and drop the files. Note that this processed locally in the browser, so you can use
relatively large files (100s of MBs).

## Notes

Since the tools are instrumentation-based file sizes necessarily become bigger when more code injected. Profiling costs some overhead, and depending on your 
choices of what to profile, can causing significant increase in load times or lag.