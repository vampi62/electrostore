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
  labels = {
    "org.opencontainers.image.url" = "https://github.com/${REPO}"
    "org.opencontainers.image.title" = "Electrostore"
    "org.opencontainers.image.description" = "Electrostore Local Image"
    "org.opencontainers.image.source" = "https://github.com/${REPO}"
    "org.opencontainers.image.version" = "${VERSION}"
  }
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

# Derive all labels
function "label" {
  params = [label_name, label_value]
  result = {
    "org.opencontainers.image.${label_name}" = "${label_value}"
  }
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
  labels = merge(
    label("url", "https://github.com/${REPO}"),
    label("title", "Electrostore api"),
    label("description", "Electrostore api image"),
    label("source", "https://github.com/${REPO}"),
    label("version", "${VERSION}")
  )
}

target "front" {
  inherits = ["defaults"]
  context = "electrostoreFRONT/"
  tags = tag("front")
  labels = merge(
    label("url", "https://github.com/${REPO}"),
    label("title", "Electrostore front"),
    label("description", "Electrostore front image"),
    label("source", "https://github.com/${REPO}"),
    label("version", "${VERSION}")
  )
}

target "ia" {
  inherits = ["defaults"]
  context = "electrostoreIA/"
  tags = tag("ia")
  labels = merge(
    label("url", "https://github.com/${REPO}"),
    label("title", "Electrostore ia"),
    label("description", "Electrostore ia image"),
    label("source", "https://github.com/${REPO}"),
    label("version", "${VERSION}")
  )
}