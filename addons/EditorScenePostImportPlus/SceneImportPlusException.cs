using System;

public class SceneImportPlusException : Exception {

    public SceneImportPlusException() {

    }

    public SceneImportPlusException(string message) : base(message) {

    }

    public SceneImportPlusException(string message, Exception innerException) : base(message, innerException) {
        
    }

}