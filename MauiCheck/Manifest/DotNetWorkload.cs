﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DotNetCheck.Manifest
{
	public partial class DotNetWorkload
	{
		[JsonProperty("workloadId")]
		public string Id { get; set; }

		[JsonProperty("packageId")]
		public string PackageId { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("workloadVersion")]
		public string WorkloadVersion { get; set; }

		[JsonProperty("workloadManifestId")]
		public string WorkloadManifestId { get; set; }

		[JsonProperty("abstract")]
		public bool Abstract { get; set; } = false;

		[JsonProperty("ignoredPackIds")]
		public List<string> IgnoredPackIds { get; set; }
	}
}
