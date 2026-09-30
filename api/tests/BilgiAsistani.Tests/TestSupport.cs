using BilgiAsistani.Core;
using Microsoft.Extensions.Options;

namespace BilgiAsistani.Tests;

public static class Pipelines
{
    public static QaPipeline Create() => new(Options.Create(new AssistantOptions()));
}
