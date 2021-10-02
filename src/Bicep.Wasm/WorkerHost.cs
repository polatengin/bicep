// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Deployments.Core.Json;
using Bicep.LanguageServer;
using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using Microsoft.JSInterop;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Bicep.Wasm
{
    public class WorkerHost
    {
        private readonly IWorkerFactory workerFactory;
        private readonly IJSRuntime jsRuntime;
        private IWorker? worker;

        public WorkerHost(IWorkerFactory workerFactory, IJSRuntime jsRuntime)
        {
            this.workerFactory = workerFactory;
            this.jsRuntime = jsRuntime;
        }

        private static IEnumerable<Assembly> GetAllDependencies(Assembly assembly)
        {
            var dict = new Dictionary<string, AssemblyName>();
            dict.Add(assembly.GetName().FullName, assembly.GetName());
            dict = GetAllDependenciesRecursive(assembly.GetName(), dict);

            return dict.Select(d => Assembly.Load(d.Value)).ToArray();
        }

        private static Dictionary<string, AssemblyName> GetAllDependenciesRecursive(AssemblyName assemblyName, Dictionary<string, AssemblyName> existingRefList)
        {
            var assembly = Assembly.Load(assemblyName);
            List<AssemblyName> a = assembly.GetReferencedAssemblies().ToList();
            foreach (var refAssemblyName in a)
            {
                if (!existingRefList.ContainsKey(refAssemblyName.FullName))
                {
                    existingRefList.Add(refAssemblyName.FullName, refAssemblyName);
                    existingRefList = GetAllDependenciesRecursive(refAssemblyName, existingRefList);
                }
            }
            return existingRefList;
        }

        public async Task InitializeAsync()
        {
            var serverAssemblies = typeof(Server).Assembly.GetReferencedAssemblies();

            worker = await workerFactory.CreateAsync();
            worker.IncomingMessage += this.ReceiveMessage;

            await worker.CreateBackgroundServiceAsync<Worker>(options => 
                options.AddConventionalAssemblyOfService()
                .AddAssemblies(new [] {
                    "Azure.Bicep.Types.Az.dll",
                    "Azure.Bicep.Types.dll",
                    "Azure.Core.dll",
                    "Azure.Deployments.Core.dll",
                    "Azure.Deployments.Expression.dll",
                    "Azure.Deployments.Templates.dll",
                    "Azure.Identity.dll",
                    "Azure.ResourceManager.Resources.dll",
                    "Azure.ResourceManager.dll",
                    "Bicep.Core.RegistryClient.dll",
                    "Bicep.Core.dll",
                    "Bicep.Decompiler.dll",
                    "Bicep.LangServer.dll",
                    "Bicep.Wasm.dll",
                    "BlazorWorker.BackgroundServiceFactory.dll",
                    "BlazorWorker.Core.dll",
                    "BlazorWorker.WorkerBackgroundService.dll",
                    "BlazorWorker.WorkerCore.dll",
                    "MediatR.dll",
                    "Microsoft.AspNetCore.Authorization.dll",
                    "Microsoft.AspNetCore.Components.Forms.dll",
                    "Microsoft.AspNetCore.Components.Web.dll",
                    "Microsoft.AspNetCore.Components.WebAssembly.dll",
                    "Microsoft.AspNetCore.Components.dll",
                    "Microsoft.AspNetCore.Metadata.dll",
                    "Microsoft.Bcl.AsyncInterfaces.dll",
                    "Microsoft.CSharp.dll",
                    "Microsoft.Extensions.Configuration.Abstractions.dll",
                    "Microsoft.Extensions.Configuration.Binder.dll",
                    "Microsoft.Extensions.Configuration.FileExtensions.dll",
                    "Microsoft.Extensions.Configuration.Json.dll",
                    "Microsoft.Extensions.Configuration.dll",
                    "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
                    "Microsoft.Extensions.DependencyInjection.dll",
                    "Microsoft.Extensions.FileProviders.Abstractions.dll",
                    "Microsoft.Extensions.FileProviders.Physical.dll",
                    "Microsoft.Extensions.FileSystemGlobbing.dll",
                    "Microsoft.Extensions.Logging.Abstractions.dll",
                    "Microsoft.Extensions.Logging.dll",
                    "Microsoft.Extensions.Options.ConfigurationExtensions.dll",
                    "Microsoft.Extensions.Options.dll",
                    "Microsoft.Extensions.Primitives.dll",
                    "Microsoft.Identity.Client.Extensions.Msal.dll",
                    "Microsoft.Identity.Client.dll",
                    "Microsoft.JSInterop.WebAssembly.dll",
                    "Microsoft.JSInterop.dll",
                    "Microsoft.VisualBasic.Core.dll",
                    "Microsoft.VisualBasic.dll",
                    "Microsoft.VisualStudio.Threading.dll",
                    "Microsoft.VisualStudio.Validation.dll",
                    "Microsoft.Win32.Primitives.dll",
                    "Microsoft.Win32.Registry.dll",
                    "Nerdbank.Streams.dll",
                    "Newtonsoft.Json.dll",
                    "OmniSharp.Extensions.JsonRpc.dll",
                    "OmniSharp.Extensions.LanguageProtocol.dll",
                    "OmniSharp.Extensions.LanguageServer.Shared.dll",
                    "OmniSharp.Extensions.LanguageServer.dll",
                    "Serialize.Linq.dll",
                    "System.AppContext.dll",
                    "System.Buffers.dll",
                    "System.Collections.Concurrent.dll",
                    "System.Collections.Immutable.dll",
                    "System.Collections.NonGeneric.dll",
                    "System.Collections.Specialized.dll",
                    "System.Collections.dll",
                    "System.ComponentModel.Annotations.dll",
                    "System.ComponentModel.DataAnnotations.dll",
                    "System.ComponentModel.EventBasedAsync.dll",
                    "System.ComponentModel.Primitives.dll",
                    "System.ComponentModel.TypeConverter.dll",
                    "System.ComponentModel.dll",
                    "System.Configuration.dll",
                    "System.Console.dll",
                    "System.Core.dll",
                    "System.Data.Common.dll",
                    "System.Data.DataSetExtensions.dll",
                    "System.Data.dll",
                    "System.Diagnostics.Contracts.dll",
                    "System.Diagnostics.Debug.dll",
                    "System.Diagnostics.DiagnosticSource.dll",
                    "System.Diagnostics.FileVersionInfo.dll",
                    "System.Diagnostics.Process.dll",
                    "System.Diagnostics.StackTrace.dll",
                    "System.Diagnostics.TextWriterTraceListener.dll",
                    "System.Diagnostics.Tools.dll",
                    "System.Diagnostics.TraceSource.dll",
                    "System.Diagnostics.Tracing.dll",
                    "System.Drawing.Primitives.dll",
                    "System.Drawing.dll",
                    "System.Dynamic.Runtime.dll",
                    "System.Formats.Asn1.dll",
                    "System.Globalization.Calendars.dll",
                    "System.Globalization.Extensions.dll",
                    "System.Globalization.dll",
                    "System.IO.Abstractions.dll",
                    "System.IO.Compression.Brotli.dll",
                    "System.IO.Compression.FileSystem.dll",
                    "System.IO.Compression.ZipFile.dll",
                    "System.IO.Compression.dll",
                    "System.IO.FileSystem.AccessControl.dll",
                    "System.IO.FileSystem.DriveInfo.dll",
                    "System.IO.FileSystem.Primitives.dll",
                    "System.IO.FileSystem.Watcher.dll",
                    "System.IO.FileSystem.dll",
                    "System.IO.IsolatedStorage.dll",
                    "System.IO.MemoryMappedFiles.dll",
                    "System.IO.Pipelines.dll",
                    "System.IO.Pipes.AccessControl.dll",
                    "System.IO.Pipes.dll",
                    "System.IO.UnmanagedMemoryStream.dll",
                    "System.IO.dll",
                    "System.Linq.Expressions.dll",
                    "System.Linq.Parallel.dll",
                    "System.Linq.Queryable.dll",
                    "System.Linq.dll",
                    "System.Memory.Data.dll",
                    "System.Memory.dll",
                    "System.Net.Http.Json.dll",
                    "System.Net.Http.dll",
                    "System.Net.HttpListener.dll",
                    "System.Net.Mail.dll",
                    "System.Net.NameResolution.dll",
                    "System.Net.NetworkInformation.dll",
                    "System.Net.Ping.dll",
                    "System.Net.Primitives.dll",
                    "System.Net.Requests.dll",
                    "System.Net.Security.dll",
                    "System.Net.ServicePoint.dll",
                    "System.Net.Sockets.dll",
                    "System.Net.WebClient.dll",
                    "System.Net.WebHeaderCollection.dll",
                    "System.Net.WebProxy.dll",
                    "System.Net.WebSockets.Client.dll",
                    "System.Net.WebSockets.dll",
                    "System.Net.dll",
                    "System.Numerics.Vectors.dll",
                    "System.Numerics.dll",
                    "System.ObjectModel.dll",
                    "System.Private.CoreLib.dll",
                    "System.Private.DataContractSerialization.dll",
                    "System.Private.Runtime.InteropServices.JavaScript.dll",
                    "System.Private.Uri.dll",
                    "System.Private.Xml.Linq.dll",
                    "System.Private.Xml.dll",
                    "System.Reactive.dll",
                    "System.Reflection.DispatchProxy.dll",
                    "System.Reflection.Emit.ILGeneration.dll",
                    "System.Reflection.Emit.Lightweight.dll",
                    "System.Reflection.Emit.dll",
                    "System.Reflection.Extensions.dll",
                    "System.Reflection.Metadata.dll",
                    "System.Reflection.Primitives.dll",
                    "System.Reflection.TypeExtensions.dll",
                    "System.Reflection.dll",
                    "System.Resources.Reader.dll",
                    "System.Resources.ResourceManager.dll",
                    "System.Resources.Writer.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll",
                    "System.Runtime.CompilerServices.VisualC.dll",
                    "System.Runtime.Extensions.dll",
                    "System.Runtime.Handles.dll",
                    "System.Runtime.InteropServices.RuntimeInformation.dll",
                    "System.Runtime.InteropServices.dll",
                    "System.Runtime.Intrinsics.dll",
                    "System.Runtime.Loader.dll",
                    "System.Runtime.Numerics.dll",
                    "System.Runtime.Serialization.Formatters.dll",
                    "System.Runtime.Serialization.Json.dll",
                    "System.Runtime.Serialization.Primitives.dll",
                    "System.Runtime.Serialization.Xml.dll",
                    "System.Runtime.Serialization.dll",
                    "System.Runtime.dll",
                    "System.Security.AccessControl.dll",
                    "System.Security.Claims.dll",
                    "System.Security.Cryptography.Algorithms.dll",
                    "System.Security.Cryptography.Cng.dll",
                    "System.Security.Cryptography.Csp.dll",
                    "System.Security.Cryptography.Encoding.dll",
                    "System.Security.Cryptography.OpenSsl.dll",
                    "System.Security.Cryptography.Primitives.dll",
                    "System.Security.Cryptography.ProtectedData.dll",
                    "System.Security.Cryptography.X509Certificates.dll",
                    "System.Security.Principal.Windows.dll",
                    "System.Security.Principal.dll",
                    "System.Security.SecureString.dll",
                    "System.Security.dll",
                    "System.ServiceModel.Web.dll",
                    "System.ServiceProcess.dll",
                    "System.Text.Encoding.CodePages.dll",
                    "System.Text.Encoding.Extensions.dll",
                    "System.Text.Encoding.dll",
                    "System.Text.Encodings.Web.dll",
                    "System.Text.Json.dll",
                    "System.Text.RegularExpressions.dll",
                    "System.Threading.Channels.dll",
                    "System.Threading.Overlapped.dll",
                    "System.Threading.Tasks.Dataflow.dll",
                    "System.Threading.Tasks.Extensions.dll",
                    "System.Threading.Tasks.Parallel.dll",
                    "System.Threading.Tasks.dll",
                    "System.Threading.Thread.dll",
                    "System.Threading.ThreadPool.dll",
                    "System.Threading.Timer.dll",
                    "System.Threading.dll",
                    "System.Transactions.Local.dll",
                    "System.Transactions.dll",
                    "System.ValueTuple.dll",
                    "System.Web.HttpUtility.dll",
                    "System.Web.dll",
                    "System.Windows.dll",
                    "System.Xml.Linq.dll",
                    "System.Xml.ReaderWriter.dll",
                    "System.Xml.Serialization.dll",
                    "System.Xml.XDocument.dll",
                    "System.Xml.XPath.XDocument.dll",
                    "System.Xml.XPath.dll",
                    "System.Xml.XmlDocument.dll",
                    "System.Xml.XmlSerializer.dll",
                    "System.Xml.dll",
                    "System.dll",
                    "WindowsBase.dll",
                    "mscorlib.dll",
                    "netstandard.dll",
                }));

            await jsRuntime.InvokeAsync<object>("LspInitialized", DotNetObjectReference.Create(this));
        }

        [JSInvokable("SendLspDataAsync")]
        public async Task SendMessage(string message)
        {
            var workerValue = worker ?? throw new InvalidOperationException($"Worker not intiialized. Did you forget to call {nameof(InitializeAsync)}?");

            await workerValue.PostMessageAsync($"SND:{message}");
        }

        public void ReceiveMessage(object? sender, string message)
        {
            if (message.StartsWith("RCV:"))
            {
                jsRuntime.InvokeVoidAsync("ReceiveLspData", message.Substring(4));
            }
        }
    }
}