// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAppGenerator;

/// <summary>
/// Interface for msapp generator factory
/// </summary>
public interface IAppGeneratorFactory
{
    /// <summary>
    /// Creates a MSApp generator
    /// </summary>
    IAppGenerator Create(bool interactive);
}
