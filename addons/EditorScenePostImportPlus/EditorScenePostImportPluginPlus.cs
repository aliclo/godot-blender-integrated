using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

public partial class EditorScenePostImportPluginPlus : EditorScenePostImportPlugin
{

    private static string _importPlusPath;

    public override void _GetImportOptions(string path)
    {
        _importPlusPath = $"{path}.import.plus";
        
        AddImportOption("Scripts", new Array<CSharpScript>());

        var plusImporterIo = new PlusImporterIO();

        try {
            var importScriptsPlus = plusImporterIo.Load(_importPlusPath);

            if(importScriptsPlus == null) {
                return;
            }

            foreach(var importScriptParams in importScriptsPlus.ImportScriptsParams) {
                foreach(var importScriptParam in importScriptParams.ImportProperties) {
                    AddImportOption($"{importScriptParams.Name}/{importScriptParam.Name}", importScriptParam.Value);
                }
            }
        } catch (SceneImportPlusException e) {
            GD.PrintErr(e.Message);
            GD.PrintErr($"Failed to import: {_importPlusPath}");
        }
    }

    public override void _PreProcess(Node scene)
    {
        var scripts = (Array<CSharpScript>) GetOptionValue("Scripts");
        var importScripts = scripts.Select(s => (EditorScenePostImportPlus) s.New().Obj).ToList();

        var defaultImportScriptsParams = importScripts.Select(s => new ImportScriptParams() {
            Name = s.GetType().Name,
            ImportProperties = s.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(ExportAttribute))).Select(ip => {
                var optionKey = $"{s.GetType().Name}/{ip.Name}";
                Variant value = GetOptionValue(optionKey);

                if(value.Obj == null) {
                    value = (Variant)typeof(Variant).GetMethod("From").MakeGenericMethod(ip.PropertyType).Invoke(null, new[] { ip.GetValue(s) });
                    GD.Print($"Using default value instead {optionKey} = {value}");
                }

                return new CustomImportProperty() {
                    Name = ip.Name,
                    Value = value
                };
            }).ToList()
        });

        var importScriptsPlus = new ImportScriptsPlus() {
            Scripts = scripts.Select(s => s.ResourcePath).ToList(),
            ImportScriptsParams = defaultImportScriptsParams.ToList()
        };

        var plusImporterIo = new PlusImporterIO();
        plusImporterIo.Save(_importPlusPath, importScriptsPlus);
    }



}
