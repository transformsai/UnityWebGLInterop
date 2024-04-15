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
      UTF8ToString(address)
      );
  },
  UnaryRequest: function (instanceKey, channelKey, serviceName, methodName, headers, base64Message, deadlineTimestampSecs) {
    return window.GrpcWebUnityDelegator.UnaryRequest(
      instanceKey, 
      channelKey, 
      UTF8ToString(serviceName), 
      UTF8ToString(methodName), 
      UTF8ToString(headers), 
      UTF8ToString(base64Message), 
      deadlineTimestampSecs);
  },
  ServerStreamingRequest: function (instanceKey, channelKey, serviceName, methodName, headers, base64Message, deadlineTimestampSecs) {
    return window.GrpcWebUnityDelegator.ServerStreamingRequest(
      instanceKey, 
      channelKey, 
      UTF8ToString(serviceName), 
      UTF8ToString(methodName), 
      UTF8ToString(headers), 
      UTF8ToString(base64Message), 
      deadlineTimestampSecs);
  },
  CancelCall: function (instanceKey, channelKey, callKey) {
    window.GrpcWebUnityDelegator.CancelCall(
      instanceKey, 
      channelKey, 
      callKey);
   },
});
