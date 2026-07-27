variable "aws_region" {
  description = "AWS region used for the deployment."
  type        = string
  default     = "ap-southeast-1"
}

variable "image_tag" {
  description = "Immutable backend image tag pushed by CI."
  type        = string
  default     = "latest"
}

variable "data_gov_api_key" {
  description = "data.gov.sg API key injected into the ECS task."
  type        = string
  sensitive   = true
}
