using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class PlusImporterIO {

    public ImportScriptsPlus Load(string path)
    {
        // Godot actually stores this in its own way for some reason, see editor_file_system.cpp::_reimport_group().
        if(!FileAccess.FileExists(path))
        {
            return null;
        }
        else
        {
            Dictionary<string, ImportScriptParams> importScriptsParamsDict = new();

            var extendedClasses = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EditorScenePostImportPlus)));

            var classNames = extendedClasses.Select(c => c.Name);

            var duplicateClassNames = classNames.Where(n => classNames.Where(on => n == on).Count() > 1);

            if(duplicateClassNames.Any()) {
                throw new SceneImportPlusException($"Failed to import. Duplicate class names extending \"{nameof(EditorScenePostImportPlus)}\": {string.Join(", ", duplicateClassNames)}");
            }

            var scripts = new List<string>();
            List<ImportScriptParams> importScriptsParams;

            using(var file = FileAccess.Open(path, FileAccess.ModeFlags.Read)) {
                bool finishedSection = false;

                string line = file.GetLine();
                bool reachedEnd = file.GetPosition() == file.GetLength();

                while(!reachedEnd) {
                    if(line == "[Scripts]") {
                        while (!reachedEnd) {
                            line = file.GetLine();
                            reachedEnd = file.GetPosition() == file.GetLength();
                            finishedSection = string.IsNullOrWhiteSpace(line);

                            if(finishedSection) {
                                break;
                            }

                            scripts.Add(line);
                        }
                    } else if (line == "[Properties]") {
                        while(!reachedEnd) {
                            line = file.GetLine();
                            reachedEnd = file.GetPosition() == file.GetLength();
                            finishedSection = string.IsNullOrWhiteSpace(line);

                            if(finishedSection) {
                                break;
                            }

                            var parameterAssignment = line.Split("=");
                            
                            var pathParameter = parameterAssignment[0];
                            var parameterValue = parameterAssignment[1];
                            
                            var parameterPathComponents = pathParameter.Split("/");
                            var scriptName = parameterPathComponents[0];
                            var parameterName = parameterPathComponents[1];

                            bool exists = importScriptsParamsDict.TryGetValue(scriptName, out ImportScriptParams importScriptParams);
                            
                            if(exists == false) {
                                importScriptParams = new ImportScriptParams() {
                                    Name = scriptName,
                                    ImportProperties = new List<CustomImportProperty>()
                                };

                                importScriptsParamsDict[scriptName] = importScriptParams;
                            }

                            var scriptType = extendedClasses.SingleOrDefault(c => c.Name.Equals(scriptName));

                            if(scriptType == null) {
                                GD.PrintErr($"Failed to find script for \"{scriptName}\", skipping");
                                continue;
                            }

                            var referencedProperty = scriptType.GetProperties()
                                .SingleOrDefault(p => Attribute.IsDefined(p, typeof(ExportAttribute)) && p.Name == parameterName);

                            if(referencedProperty == null) {
                                GD.PrintErr($"Failed to import \"{parameterName}\" for script \"{scriptName}\", it doesn't exist anymore, skipping");
                                continue;
                            }

                            importScriptParams.ImportProperties.Add(new CustomImportProperty() {
                                Name = parameterName,
                                Value = (Variant)typeof(Variant).GetMethod("From").MakeGenericMethod(referencedProperty.PropertyType).Invoke(null, new[] { Convert.ChangeType(parameterValue, referencedProperty.PropertyType) })
                            });
                        }
                    } else {
                        line = file.GetLine();
                        reachedEnd = file.GetPosition() == file.GetLength();
                    }
                }
            }

            importScriptsParams = importScriptsParamsDict.Values.ToList();

            return new ImportScriptsPlus() {
                Scripts = scripts,
                ImportScriptsParams = importScriptsParams
            };
        }
    }

    public void Save(string path, ImportScriptsPlus importScriptsPlus)
    {
        // Godot actually stores this in its own way for some reason, see editor_file_system.cpp::_reimport_group().
        using(var file = FileAccess.Open(path, FileAccess.ModeFlags.Write)) {
            file.StoreLine("[Scripts]");

            foreach(var script in importScriptsPlus.Scripts) {
                file.StoreLine(script);
            }

            file.StoreLine("");

            file.StoreLine("[Properties]");

            foreach(var importScriptParam in importScriptsPlus.ImportScriptsParams) {
                foreach(var defaultImportScriptParam in importScriptParam.ImportProperties) {
                    file.StoreLine($"{importScriptParam.Name}/{defaultImportScriptParam.Name}={defaultImportScriptParam.Value}");
                }
            }

            file.StoreLine("");
        }
    }

}