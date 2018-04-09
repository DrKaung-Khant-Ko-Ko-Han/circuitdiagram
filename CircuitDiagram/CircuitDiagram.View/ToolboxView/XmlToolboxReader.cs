﻿// Circuit Diagram http://www.circuit-diagram.org/
// 
// Copyright (C) 2016  Samuel Fisher
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using CircuitDiagram.Circuit;
using CircuitDiagram.Logging;
using CircuitDiagram.TypeDescription;
using CircuitDiagram.TypeDescriptionIO.Util;
using CircuitDiagram.View.Services;
using Microsoft.Extensions.Logging;

namespace CircuitDiagram.View.ToolboxView
{
    class XmlToolboxReader : IToolboxReader
    {
        private static readonly ILogger Log = LogManager.GetLogger<XmlToolboxReader>();

        private readonly IComponentIconProvider iconProvider;

        public XmlToolboxReader(IComponentIconProvider iconProvider)
        {
            this.iconProvider = iconProvider;
        }

        public ToolboxEntry[][] GetToolbox(Stream input, IReadOnlyCollection<ComponentDescription> availableDescriptions)
        {
            var doc = XDocument.Load(input);

            var entries = new List<ToolboxEntry[]>();
            foreach (var category in doc.Root.Elements("category"))
            {
                var categoryItems = new List<ToolboxEntry>();

                foreach (var item in category.Elements("component"))
                {
                    string id = item.Attribute("guid")?.Value;
                    if (id == null)
                        continue;

                    Guid guid;
                    if (!Guid.TryParse(id, out guid))
                        continue;

                    string configurationName = item.Attribute("configuration")?.Value;
                    string keyName = item.Attribute("key")?.Value;
                    Key? key = null;
                    Key keyValue;
                    if (Enum.TryParse(keyName, true, out keyValue))
                        key = keyValue;

                    var type = availableDescriptions.FirstOrDefault(x => x.ID == guid.ToString());
                    if (type == null)
                    {
                        Log.LogWarning($"Unable to find a component type with GUID '{guid}'. Hiding from the toolbox.");
                        continue;
                    }

                    var configuration = type.Metadata.Configurations.FirstOrDefault(x => x.Name == configurationName);

                    var entry = new ToolboxEntry
                    {
                        Name = configuration?.Name ?? type.ComponentName,
                        Type = configuration?.GetComponentType(type) ?? type.GetComponentTypes().First(),
                        Configuration = configuration.Name,
                        Key = key
                    };
                    entry.Icon = iconProvider.GetIcon(Tuple.Create(entry.Type.Id, entry.Configuration));

                    categoryItems.Add(entry);
                }

                if (categoryItems.Any())
                    entries.Add(categoryItems.ToArray());
            }

            return entries.ToArray();
        }
    }
}
