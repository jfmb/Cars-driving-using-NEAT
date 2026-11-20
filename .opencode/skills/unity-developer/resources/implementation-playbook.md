# Unity Developer Implementation Playbook

## Project: AI-Prototype
## Skill Version: 1.0.0

## Quick Reference Commands

### Unity Project Setup
```bash
# Verify OpenCode is active
echo $OPENCODE

# List available skills
opencode skills list

# Load unity-developer skill
opencode skill unity-developer
```

### Common Workflows

#### 1. Performance Analysis
```bash
# Open Unity Profiler
# Check CPU/GPU/memory usage
# Profile on target platform
```

#### 2. Build Optimization
```bash
# Configure build targets
# Optimize player settings
# Set up addressable assets
```

#### 3. Cross-Platform Testing
```bash
# Test on multiple platforms
# Verify input systems
# Check graphics API compatibility
```

### Architecture Patterns

#### ECS Implementation
- Entity/Component/System separation
- SharedComponent usage
- Buffer and Group patterns
- EntityManager operations

#### Rendering Pipelines
- URP vs HDRP selection
- Custom render features
- Shader Graph workflows
- Lighting baking settings

#### UI System
- UI Toolkit vs uGUI selection
- Canvas Scaler configuration
- Event system setup
- Localization support

### Performance Checklists

#### CPU Optimization
- [ ] Profile with Unity Profiler
- [ ] Check Job System usage
- [ ] Verify Burst compilation
- [ ] Review GC allocation patterns

#### GPU Optimization
- [ ] Profile with Frame Debugger
- [ ] Check draw calls
- [ ] Verify shader complexity
- [ ] Optimize texture usage

#### Memory Optimization
- [ ] Profile with Memory Profiler
- [ ] Check native allocations
- [ ] Verify object pooling
- [ ] Review asset loading patterns

### Platform-Specific Settings

#### Mobile (iOS/Android)
- [ ] Enable Metal/GLES APIs
- [ ] Optimize texture formats
- [ ] Configure input system
- [ ] Set up addressable loading

#### Console
- [ ] Verify certification requirements
- [ ] Configure platform-specific features
- [ ] Set up dedicated server if needed
- [ ] Test controller input

#### WebGL
- [ ] Optimize for browser constraints
- [ ] Configure template
- [ ] Set up streaming assets
- [ ] Test input fallback

### Debugging Tools

#### Unity Profiler
- CPU Usage tracking
- GPU Rendering stats
- Memory Allocation
- Thread usage

#### Frame Debugger
- Capture frames
- Analyze draw calls
- Verify shader compilation
- Check texture formats

#### Logs and Analytics
- Debug log configuration
- Error reporting
- Player metrics
- Crash reports