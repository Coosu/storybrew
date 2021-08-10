using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorybrewEditor.Processes
{
    static class ProcessDevenv
    {
        private static Process process;

        public static void OpenScriptFile(string scriptFile)
        {
            // multi instance: not supported
            //
            // with no instance
            //.\devenv.exe "$(SolutionDir)\storyboard.sln" /command "File.OpenFile $(ScriptName).cs" /nosplash
            // with instance
            //.\devenv.exe "$(ScriptDir)\$(ScriptName).cs" /edit 
        }
    }
}
