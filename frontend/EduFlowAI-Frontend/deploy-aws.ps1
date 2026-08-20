# deploy-aws.ps1 - Automated Build & High-Performance S3/CloudFront Deployment
param(
    [string]$BucketName = "eduflowai-frontend-803109509063",
    [string]$DistributionId = "E2SZV9DNH56MS5"
)

Write-Host "Building EduFlowAI Angular Frontend for Production..." -ForegroundColor Green
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Aborting deployment." -ForegroundColor Red
    exit 1
}

Write-Host "Syncing static hashed assets with long-term caching (max-age=31536000, immutable)..." -ForegroundColor Green
aws s3 sync dist/EduFlowAI-Frontend/browser s3://$BucketName --delete --exclude "index.html" --cache-control "public, max-age=31536000, immutable"

Write-Host "Syncing index.html with no-cache header for instant updates..." -ForegroundColor Green
aws s3 cp dist/EduFlowAI-Frontend/browser/index.html s3://$BucketName/index.html --cache-control "no-cache, no-store, must-revalidate"

if ($DistributionId) {
    Write-Host "Invalidating CloudFront cache for Distribution: $DistributionId..." -ForegroundColor Green
    aws cloudfront create-invalidation --distribution-id $DistributionId --paths "/*"
}

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Live URL: https://dl96mzyzn889l.cloudfront.net" -ForegroundColor Cyan