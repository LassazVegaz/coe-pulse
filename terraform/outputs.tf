output "api_url" {
  description = "Public URL for the backend."
  value       = "http://${aws_lb.api.dns_name}"
}

output "ecr_repository_url" {
  description = "ECR repository used by the deployment workflow."
  value       = aws_ecr_repository.api.repository_url
}
