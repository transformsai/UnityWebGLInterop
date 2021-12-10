mergeInto(LibraryManager.library, {
  RegisterInstance: function () {
    const objName = "GrpcWebUnityConnector";

    const register = function () {
      window.GrpcWebUnityDelegator.RegisterInstance(Module, objName);
    };

    if (window.GrpcWebUnityDelegator) {
      register();
    } else {
      var scriptTag = document.getElementById(objName);
      if (!scriptTag) {
        scriptTag = document.createElement("script");
        scriptTag.id = objName;
        scriptTag.src = "./GrpcWebUnity.js";
        scriptTag.onload = register;
        document.body.appendChild(scriptTag);
      } else {
        scriptTag.onload = register;
      }
    }
    const bufferSize = lengthBytesUTF8(objName) + 1;
    const buffer = _malloc(bufferSize);
    stringToUTF8(objName, buffer, bufferSize);
    return buffer;
  },
  RegisterChannel: function (instanceKey, address) {
    return window.GrpcWebUnityDelegator.RegisterChannel(
      instanceKey, 
      Pointer_stringify(address)
      );
  },
  UnaryRequest: function (instanceKey, channelKey, serviceName, methodName, headers, base64Message, deadlineTimestampSecs) {
    return window.GrpcWebUnityDelegator.UnaryRequest(
      instanceKey, 
      channelKey, 
      Pointer_stringify(serviceName), 
      Pointer_stringify(methodName), 
      Pointer_stringify(headers), 
      Pointer_stringify(base64Message), 
      deadlineTimestampSecs);
  },
  ServerStreamingRequest: function (instanceKey, channelKey, serviceName, methodName, headers, base64Message, deadlineTimestampSecs) {
    return window.GrpcWebUnityDelegator.ServerStreamingRequest(
      instanceKey, 
      channelKey, 
      Pointer_stringify(serviceName), 
      Pointer_stringify(methodName), 
      Pointer_stringify(headers), 
      Pointer_stringify(base64Message), 
      deadlineTimestampSecs);
  },
  CancelCall: function (instanceKey, channelKey, callKey) {
    window.GrpcWebUnityDelegator.CancelCall(
      instanceKey, 
      channelKey, 
      callKey);
   },
});
