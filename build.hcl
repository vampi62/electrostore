# build-ci.hcl
#
# only used for Github actions workflow.
# For locally building images use build.hcl
#
# For more information on buildx bake file definition see:
# https://github.com/docker/buildx/blob/master/docs/bake-reference.md
# https://github.com/docker/buildx/blob/master/docs/reference/buildx_bake.md
#
# NOTE: You can only run this from the root folder.
#-----------------------------------------------------------------------------------------
# (Environment) input variables
# If the env var is not set, then the default value is used
#-----------------------------------------------------------------------------------------
variable "REPO" {
  default = "vampi62/electrostore"
}
variable "VERSION" {
  default = "local"
}

#-----------------------------------------------------------------------------------------
# Grouping of targets to build. All these images are built when using:
# docker buildx bake -f tests\build.hcl
#-----------------------------------------------------------------------------------------
group "default" {
  targets = [
    "api",
    "front",
    "ia"
  ]
}

#-----------------------------------------------------------------------------------------
# Default settings that will be inherited by all targets (images to build).
#-----------------------------------------------------------------------------------------
target "defaults" {
  platforms = [ "linux/amd64"]
  dockerfile = "Dockerfile"
}

#-----------------------------------------------------------------------------------------
# User defined functions
#------------------------------------------------------------------------------------------
# Derive all tags
function "tag" {
  params = [image_name]
  result = [
    "ghcr.io/${REPO}/${image_name}:${VERSION}",
    "ghcr.io/${REPO}/${image_name}:latest"
  ]
}

#-----------------------------------------------------------------------------------------
# All individual targets (images to build)
# Build an individual target using.
# docker buildx bake -f tests\build.hcl <target>
# E.g. to build target front
# docker buildx bake -f tests\build.hcl front
#-----------------------------------------------------------------------------------------

target "api" {
  inherits = ["defaults"]
  context = "electrostoreAPI/"
  tags = tag("api")
}

target "front" {
  inherits = ["defaults"]
  context = "electrostoreFRONT/"
  tags = tag("front")
}

target "ia" {
  inherits = ["defaults"]
  context = "electrostoreIA/"
  tags = tag("ia")
}