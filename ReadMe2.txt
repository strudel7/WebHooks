Nuget
-----
To get all the packages listed in the various package.config files simply run the following from the Package Manager Console.

  >> Update-Package

Delay Signing
-------------
The assemblies are delay signed so that means they have a public key to pair with the private key
the assembly is signed with.  When the assembly is loaded there is a check to see if the assembly 
is signed in correspondence to the public key.  When its not an exception occurs such as 

  "Could not load file or assembly 'Microsoft.AspNet.WebHooks.Common.dll ...' or one of its 
   dependencies. Strong name validation failed. (Exception from HRESULT: 0x8013141A)"
   
To disable this validation you 1) first need the public key and 2) then tell the system to NOT
verify assemblies that have it.  You run the sn utility as a Administrator to do both these things.

  1. >> sn -T D:\PROJECTS\WebHooks\bin\Debug\Microsoft.AspNet.WebHooks.Common.dll
  
     Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.0
     Copyright (c) Microsoft Corporation.  All rights reserved.

     Public key token is 31bf3856ad364e35
     
  2. >> sn -Vr *,31bf3856ad364e35

     Microsoft (R) .NET Framework Strong Name Utility  Version 4.0.30319.0
     Copyright (c) Microsoft Corporation.  All rights reserved.

     Verification entry added for assembly '*,31bf3856ad364e35'
     


ngrok 
-----
To get ngrok to work you have to specify the "host-header" such as 

  >> ngrok http [port] -host-header="localhost:[port]"


http://stackoverflow.com/questions/30535336/exposing-localhost-to-the-internet-via-tunneling-using-ngrok-http-error-400
