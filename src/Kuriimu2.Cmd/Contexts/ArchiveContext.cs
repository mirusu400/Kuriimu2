﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kontract;
using Kontract.Extensions;
using Kontract.Interfaces.FileSystem;
using Kontract.Interfaces.Managers;
using Kontract.Interfaces.Plugins.State;
using Kontract.Models;
using Kontract.Models.IO;
using Kore.Factories;
using Kore.Managers.Plugins;

namespace Kuriimu2.Cmd.Contexts
{
    class ArchiveContext : BaseFileContext
    {
        private readonly IStateInfo _stateInfo;
        private readonly IArchiveState _archiveState;
        private readonly IFileSystem _archiveFileSystem;
        private readonly IContext _parentContext;

        protected override IList<Command> Commands { get; } = new List<Command>
        {
            new Command("open", "file"),
            new Command("open-with", "file", "plugin-id"),
            new Command("save", "file-index"),
            new Command("close","file-index"),
            new Command("close-all"),
            new Command("select","file-index"),
            new Command("list"),
            new Command("list-open"),
            new Command("back")
        };

        public ArchiveContext(IStateInfo stateInfo, IContext parentContext, IInternalPluginManager pluginManager) : base(pluginManager)
        {
            ContractAssertions.IsNotNull(stateInfo, nameof(stateInfo));
            ContractAssertions.IsNotNull(parentContext, nameof(parentContext));

            _stateInfo = stateInfo;
            _archiveState = _stateInfo.PluginState as IArchiveState;
            _archiveFileSystem = FileSystemFactory.CreateAfiFileSystem(stateInfo);
            _parentContext = parentContext;
        }

        protected override async Task<IContext> ExecuteNextInternal(Command command, IList<string> arguments)
        {
            var executeContext = await base.ExecuteNextInternal(command, arguments);
            if (executeContext != null)
                return executeContext;

            switch (command.Name)
            {
                case "list":
                    ListFiles(_archiveFileSystem, UPath.Root);
                    return this;

                case "back":
                    return _parentContext;
            }

            return null;
        }

        protected override bool FileExists(string filePath)
        {
            return _archiveFileSystem.FileExists(new UPath(filePath).ToAbsolute());
        }

        protected override bool IsLoaded(string filePath)
        {
            var absolutePath = _stateInfo.AbsoluteDirectory / _stateInfo.FilePath / filePath;
            return PluginManager.IsLoaded(absolutePath);
        }

        protected override Task<LoadResult> LoadFileInternal(string filePath, Guid pluginId)
        {
            var absoluteFilePath = new UPath(filePath).ToAbsolute();
            var selectedAfi = _archiveState.Files.First(x => x.FilePath == absoluteFilePath);

            // Try every preset plugin first
            foreach (var selectedAfiPluginId in selectedAfi.PluginIds ?? Array.Empty<Guid>())
            {

            }

            return pluginId == Guid.Empty ?
                PluginManager.LoadFile(_stateInfo, selectedAfi) :
                PluginManager.LoadFile(_stateInfo, selectedAfi, pluginId);
        }

        private void ListFiles(IFileSystem fileSystem, UPath listPath, int iteration = 1)
        {
            var prefix = new string(' ', iteration * 2);
            Console.WriteLine(prefix + (iteration == 1 ? _stateInfo.FilePath.ToRelative() : listPath.GetName()));

            // Print files
            foreach (var file in fileSystem.EnumeratePaths(listPath, "*", SearchOption.TopDirectoryOnly, SearchTarget.File))
                Console.WriteLine(prefix + file.GetName());

            // Print directories
            foreach (var dir in fileSystem.EnumeratePaths(listPath, "*", SearchOption.AllDirectories, SearchTarget.Directory))
                if (listPath != dir && listPath == dir.GetDirectory())
                    ListFiles(fileSystem, dir, iteration + 1);
        }
    }
}