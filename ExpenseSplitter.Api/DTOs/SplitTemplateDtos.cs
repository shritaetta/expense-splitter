using System;
using System.Collections.Generic;

namespace ExpenseSplitter.Api.DTOs
{
    public class CreateSplitTemplateDto
    {
        public required string Name { get; set; }
        public List<CreateSplitTemplateItemDto> Items { get; set; } = new List<CreateSplitTemplateItemDto>();
    }

    public class CreateSplitTemplateItemDto
    {
        public Guid UserId { get; set; }
        public decimal ShareValue { get; set; }
    }

    public class SplitTemplateDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public required string Name { get; set; }
        public List<SplitTemplateItemDto> Items { get; set; } = new List<SplitTemplateItemDto>();
    }

    public class SplitTemplateItemDto
    {
        public Guid UserId { get; set; }
        public decimal ShareValue { get; set; }
    }
}
