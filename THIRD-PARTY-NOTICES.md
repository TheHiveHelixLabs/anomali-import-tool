# Third-Party Notices

This file contains notices for third-party software components used in the Anomali Threat Bulletin Import Tool.

## Overview

This software incorporates components from various open source projects. This document lists the licenses and notices for these components as required by their respective licenses.

---

## .NET Libraries

### Microsoft.Extensions.DependencyInjection
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/extensions
- **Used for**: Dependency injection container

### Microsoft.Extensions.Logging
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/extensions
- **Used for**: Logging framework

### Microsoft.Extensions.Configuration
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/extensions
- **Used for**: Configuration management

### Serilog
- **License**: Apache License 2.0
- **Copyright**: © Serilog Contributors
- **URL**: https://github.com/serilog/serilog
- **Used for**: Structured logging

### Newtonsoft.Json
- **License**: MIT License
- **Copyright**: © James Newton-King
- **URL**: https://github.com/JamesNK/Newtonsoft.Json
- **Used for**: JSON serialization/deserialization

---

## Document Processing Libraries

### iTextSharp/iText7
- **License**: AGPL/Commercial License
- **Copyright**: © iText Group NV
- **URL**: https://github.com/itext/itext7-dotnet
- **Used for**: PDF document processing
- **Note**: Commercial license required for commercial use

### DocumentFormat.OpenXml
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/OfficeDev/Open-XML-SDK
- **Used for**: Microsoft Office document processing

### EPPlus
- **License**: Polyform Noncommercial License 1.0.0/Commercial License
- **Copyright**: © EPPlus Software AB
- **URL**: https://github.com/EPPlusSoftware/EPPlus
- **Used for**: Excel file processing
- **Note**: Commercial license required for commercial use

---

## OCR Libraries

### Tesseract.NET
- **License**: Apache License 2.0
- **Copyright**: © Tesseract OCR contributors
- **URL**: https://github.com/charlesw/tesseract
- **Used for**: Optical character recognition

### Windows.Media.Ocr
- **License**: Microsoft Software License
- **Copyright**: © Microsoft Corporation
- **Used for**: Windows OCR capabilities

---

## UI Libraries

### Microsoft.WindowsAPICodePack
- **License**: Microsoft Software License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/aybe/Windows-API-Code-Pack-1.1
- **Used for**: Windows shell integration

### WPF Extended Toolkit
- **License**: Microsoft Public License (Ms-PL)
- **Copyright**: © Xceed Software Inc.
- **URL**: https://github.com/xceedsoftware/wpftoolkit
- **Used for**: Extended WPF controls

---

## HTTP and Networking

### Polly
- **License**: BSD 3-Clause License
- **Copyright**: © App vNext
- **URL**: https://github.com/App-vNext/Polly
- **Used for**: Resilience and transient-fault-handling

### RestSharp
- **License**: Apache License 2.0
- **Copyright**: © RestSharp contributors
- **URL**: https://github.com/restsharp/RestSharp
- **Used for**: REST API client

---

## Testing Libraries

### MSTest.TestFramework
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/microsoft/testfx
- **Used for**: Unit testing framework

### Moq
- **License**: BSD 3-Clause License
- **Copyright**: © Daniel Cazzulino, kzu, Moq contributors
- **URL**: https://github.com/moq/moq4
- **Used for**: Mocking framework for testing

### FluentAssertions
- **License**: Apache License 2.0
- **Copyright**: © Dennis Doomen, Jonas Nyrup
- **URL**: https://github.com/fluentassertions/fluentassertions
- **Used for**: Fluent test assertions

---

## Security Libraries

### BCrypt.Net
- **License**: MIT License
- **Copyright**: © Ryan D. Emerle
- **URL**: https://github.com/BcryptNet/bcrypt.net
- **Used for**: Password hashing

### System.Security.Cryptography
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **Used for**: Cryptographic operations

---

## Database Libraries

### Microsoft.Data.Sqlite
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/efcore
- **Used for**: SQLite database access

### Entity Framework Core
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **URL**: https://github.com/dotnet/efcore
- **Used for**: Object-relational mapping

---

## License Texts

### MIT License
```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Apache License 2.0
```
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

---

## Commercial License Notice

Some components used in this software require commercial licenses for commercial use:

- **iText7**: Requires commercial license for commercial applications
- **EPPlus**: Requires commercial license for commercial applications

Please ensure compliance with these licensing requirements for your use case.

---

## Disclaimer

This list may not be exhaustive. The actual third-party components and their licenses may vary based on the specific build configuration and version of the software. Always refer to the package.json, packages.config, or project files for the most current list of dependencies.

For questions about licensing or to report missing attributions, please contact the project maintainers.

---

**Last Updated**: January 2025  
**Version**: 1.0.0 