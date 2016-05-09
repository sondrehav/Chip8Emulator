using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Core.Graphics
{
    public class ShaderBase
    {

        protected readonly int program;

        public readonly string vertexShaderPath, fragmentShaderPath;

        // Creates shader from vertex and fragment source.
        public ShaderBase(string vertexShader, string fragmentShader)
        {

            SingleShader vs = CreateShader(vertexShader, ShaderType.VertexShader);
            SingleShader fs = CreateShader(fragmentShader, ShaderType.FragmentShader);
            program = GL.CreateProgram();
            GL.AttachShader(program, vs.id);
            GL.AttachShader(program, fs.id);
            GL.LinkProgram(program);
            
            GL.ValidateProgram(program);

            vertexShaderPath = vertexShader;
            fragmentShaderPath = fragmentShader;

            globalShaderList.Add(this);

        }

        public void Bind()
        {
            GL.UseProgram(program);
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        private Dictionary<string, int> uniforms = new Dictionary<string, int>();

        public void UniformMatrix4(string uniform, Matrix4 matrix, bool transpose = false)
        {
            if (!uniforms.ContainsKey(uniform)) uniforms[uniform] = GL.GetUniformLocation(this.program, uniform);
            int location = uniforms[uniform];
            GL.UniformMatrix4(location, transpose, ref matrix);
        }

        public void UniformVec3(string uniform, Vector3 vec)
        {
            if (!uniforms.ContainsKey(uniform)) uniforms[uniform] = GL.GetUniformLocation(this.program, uniform);
            int location = uniforms[uniform];
            GL.Uniform3(location, vec.X, vec.Y, vec.Z);
        }

        public void Uniform1i(string uniform, int value)
        {
            if (!uniforms.ContainsKey(uniform)) uniforms[uniform] = GL.GetUniformLocation(this.program, uniform);
            int location = uniforms[uniform];
            GL.Uniform1(location, value);
        }

        public void Uniform1f(string uniform, float value)
        {
            if (!uniforms.ContainsKey(uniform)) uniforms[uniform] = GL.GetUniformLocation(this.program, uniform);
            int location = uniforms[uniform];
            GL.Uniform1(location, value);
        }

        private static Regex regex = new Regex("^\\#input\\s(vertex|normal|tangent|color|uv0|uv1|uv2|uv3)\\s(.*)");

        private struct SingleShader
        {
            public int id;
        }

        private static Dictionary<string, SingleShader> shaders = new Dictionary<string, SingleShader>();

        private SingleShader CreateShader(string filename, ShaderType type)
        {
            if (shaders.ContainsKey(filename)) return shaders[filename];
            string shaderSource;
            using (StreamReader reader = new StreamReader(filename))
            {
                StringBuilder shaderSourceBuilder = new StringBuilder();
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    shaderSourceBuilder.Append(line + "\n");
                }
                reader.Close();
                shaderSource = shaderSourceBuilder.ToString();
            }
            int s_id = GL.CreateShader(type);
            GL.ShaderSource(s_id, shaderSource);
            GL.CompileShader(s_id);
            string infoLog = GL.GetShaderInfoLog(s_id);
            int statusCode;
            GL.GetShader(s_id, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1)
            {
                Console.WriteLine("ERROR: Failed to compile shader '" + filename + "'.");
                Console.WriteLine(infoLog);
                Console.WriteLine("SOURCE CODE: \n\n");
                int lineNum = 1;
                foreach (string s in shaderSource.Split('\n'))
                {
                    Console.WriteLine(lineNum.ToString() + ".\t" + s);
                    lineNum++;
                }
                GL.DeleteShader(s_id);
                s_id = -1;
                throw new Exception();
            }


            // glGetObjectParameteriv(shaderID, GL_OBJECT_ACTIVE_UNIFORMS, &count);
            // for i in 0 to count:
            // glGetActiveUniform(shaderID, i, bufSize, &length, &size, &type, name);

            /*int size;
            

            int uniformSize;
            ActiveUniformType uniformType;
            
            GL.GetActiveUniform(s_id, 0, out uniformSize, out uniformType);*/

            SingleShader s_;
            s_.id = s_id;
            shaders[filename] = s_;
            return s_;
        }


        private static List<ShaderBase> globalShaderList = new List<ShaderBase>();

        public static void SetGlobalMatrix(string uniform, Matrix4 matrix, bool transpose = false)
        {
            foreach (ShaderBase s in globalShaderList)
            {
                s.Bind();
                s.UniformMatrix4(uniform, matrix, transpose);
                s.Unbind();
            }
        }

        public static void SetGlobalVec3(string uniform, Vector3 vec)
        {
            foreach (ShaderBase s in globalShaderList)
            {
                s.Bind();
                s.UniformVec3(uniform, vec);
                s.Unbind();
            }
        }

    }
}
